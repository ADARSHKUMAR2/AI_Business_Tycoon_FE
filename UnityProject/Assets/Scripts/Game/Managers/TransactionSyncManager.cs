using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;
using System.Linq;
using Newtonsoft.Json; // 1. Added Newtonsoft

namespace AIBusinessTycoon.Managers
{
    [RequireComponent(typeof(StoreInteractionManager))]
    public class TransactionSyncManager : MonoBehaviour
    {
        private StoreInteractionManager store;
        private TransactionBatchData currentBatch;

        [SerializeField] private float syncIntervalSeconds = 10f;
        private bool isSyncing = false;
        private int retryAttempt;
        private float nextRetryTime;
        private const int MaxRetryAttempts = 6;
        private const float MaxRetryDelaySeconds = 60f;
        private string PendingBatchKey => $"tycoon_pending_transactions_{store?.BusinessData?.business_id}";

        private void Awake()
        {
            store = GetComponent<StoreInteractionManager>();
            currentBatch = new TransactionBatchData();
        }

        private void Start()
        {
            currentBatch = LoadPendingBatch() ?? currentBatch;
            StartCoroutine(SyncRoutine());
        }

        public void RecordSale(List<string> itemKeys, float revenue)
        {
            foreach(var item in itemKeys)
            {
                if (currentBatch.items_sold.ContainsKey(item))
                    currentBatch.items_sold[item]++;
                else
                    currentBatch.items_sold[item] = 1;
            }

            currentBatch.total_revenue += revenue;
            currentBatch.total_customers_served++;
            SavePendingBatch();
        }

        public void RecordRestock(string itemKey, int quantity)
        {
            if (currentBatch.items_restocked.ContainsKey(itemKey))
                currentBatch.items_restocked[itemKey] += quantity;
            else
                currentBatch.items_restocked[itemKey] = quantity;
            SavePendingBatch();
        }

        private IEnumerator SyncRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(syncIntervalSeconds);

                if (Time.unscaledTime < nextRetryTime)
                    continue;

                if ((currentBatch.total_customers_served > 0 || currentBatch.items_restocked.Count > 0) && !isSyncing && GameManager.Instance.CurrentPlayer != null)
                {
                    isSyncing = true;

                    TransactionBatchData batchToSend = currentBatch;
                    currentBatch = new TransactionBatchData();
                    Debug.Log(
                        "[ShelfStockTrace][SyncRequest] business=" + store.BusinessData.business_id +
                        " batchId=" + batchToSend.batch_id +
                        " sold=[" + string.Join(", ", batchToSend.items_sold.Select(item => item.Key + "=" + item.Value)) + "]" +
                        " localStock=[" + string.Join(", ", store.BusinessData.inventory.Select(item => item.Key + "=" + item.Value.stock)) + "]",
                        this);

                    // 2. Replaced the manual serialization with Newtonsoft!
                    string jsonPayload = JsonConvert.SerializeObject(batchToSend);

                    string playerId = GameManager.Instance.CurrentPlayer.player_id;
                    string businessId = store.BusinessData.business_id;
                    string url = $"{TycoonAPIService.Instance.GetBaseURL()}/api/game/business/{playerId}/{businessId}/sync_transactions";

                    StartCoroutine(TycoonAPIService.Instance.PostRequestRawWithRetry(url, jsonPayload,
                        (response) => {
                            BusinessData syncedBusiness = JsonConvert.DeserializeObject<BusinessData>(response);
                            if (syncedBusiness != null)
                            {
                                Debug.Log(
                                    "[ShelfStockTrace][SyncResponse] business=" + syncedBusiness.business_id +
                                    " serverStock=[" + string.Join(", ", syncedBusiness.inventory.Select(item => item.Key + "=" + item.Value.stock)) + "]",
                                    this);
                                store.BusinessData = syncedBusiness;
                                RebindShelvesToInventory(syncedBusiness);
                            }
                            DeletePendingBatch();
                            retryAttempt = 0;
                            nextRetryTime = 0f;
                            isSyncing = false;
                        },
                        (err) => {
                            Debug.LogError($"[SyncManager] Failed to sync batch: {err}");
                            MergeFailedBatch(batchToSend);
                            SavePendingBatch();
                            retryAttempt = Mathf.Min(retryAttempt + 1, MaxRetryAttempts);
                            float backoff = Mathf.Min(
                                MaxRetryDelaySeconds,
                                Mathf.Pow(2f, retryAttempt));
                            nextRetryTime = Time.unscaledTime + backoff + UnityEngine.Random.Range(0f, 1f);
                            isSyncing = false;
                        }
                    ));
                }
            }
        }

        private void RebindShelvesToInventory(BusinessData updatedBusiness)
        {
            foreach (InteractableShelf shelf in store.GetComponentsInChildren<InteractableShelf>(true))
            {
                if (updatedBusiness.inventory != null &&
                    updatedBusiness.inventory.TryGetValue(shelf.itemKey, out InventoryItem updatedItem))
                {
                    shelf.RebindInventoryItem(updatedItem);
                }
                else
                {
                    Debug.LogWarning(
                        "[ShelfStockTrace][ShelfRebindMissing] business=" + updatedBusiness.business_id +
                        " shelf=" + shelf.name + " item=" + shelf.itemKey + " not found in synced inventory",
                        shelf);
                }
            }
        }

        private void SavePendingBatch()
        {
            if (store == null || store.BusinessData == null) return;
            PlayerPrefs.SetString(PendingBatchKey, JsonConvert.SerializeObject(currentBatch));
            PlayerPrefs.Save();
        }

        private TransactionBatchData LoadPendingBatch()
        {
            if (store == null || store.BusinessData == null || !PlayerPrefs.HasKey(PendingBatchKey))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<TransactionBatchData>(PlayerPrefs.GetString(PendingBatchKey));
            }
            catch (Exception error)
            {
                Debug.LogError($"[SyncManager] Failed to load pending batch: {error.Message}");
                return null;
            }
        }

        private void DeletePendingBatch()
        {
            PlayerPrefs.DeleteKey(PendingBatchKey);
            PlayerPrefs.Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SavePendingBatch();
        }

        private void OnApplicationQuit()
        {
            SavePendingBatch();
        }

        // 3. Deleted SerializeBatch() method entirely.

        private void MergeFailedBatch(TransactionBatchData failed)
        {
            currentBatch.total_revenue += failed.total_revenue;
            currentBatch.total_customers_served += failed.total_customers_served;
            foreach(var kvp in failed.items_sold)
            {
                if (currentBatch.items_sold.ContainsKey(kvp.Key))
                    currentBatch.items_sold[kvp.Key] += kvp.Value;
                else
                    currentBatch.items_sold[kvp.Key] = kvp.Value;
            }
            foreach(var kvp in failed.items_restocked)
            {
                if (currentBatch.items_restocked.ContainsKey(kvp.Key))
                    currentBatch.items_restocked[kvp.Key] += kvp.Value;
                else
                    currentBatch.items_restocked[kvp.Key] = kvp.Value;
            }
        }
    }
}
