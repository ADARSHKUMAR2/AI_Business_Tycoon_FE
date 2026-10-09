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

        private IEnumerator SyncRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(syncIntervalSeconds);

                if (currentBatch.total_customers_served > 0 && !isSyncing && GameManager.Instance.CurrentPlayer != null)
                {
                    isSyncing = true;

                    TransactionBatchData batchToSend = currentBatch;

                    // 2. Replaced the manual serialization with Newtonsoft!
                    string jsonPayload = JsonConvert.SerializeObject(batchToSend);

                    string playerId = GameManager.Instance.CurrentPlayer.player_id;
                    string businessId = store.BusinessData.business_id;
                    string url = $"{TycoonAPIService.Instance.GetBaseURL()}/api/game/business/{playerId}/{businessId}/sync_transactions";

                    StartCoroutine(TycoonAPIService.Instance.PostRequestRaw(url, jsonPayload,
                        (response) => {
                            BusinessData syncedBusiness = JsonConvert.DeserializeObject<BusinessData>(response);
                            if (syncedBusiness != null)
                                store.BusinessData = syncedBusiness;
                            currentBatch = new TransactionBatchData();
                            DeletePendingBatch();
                            isSyncing = false;
                        },
                        (err) => {
                            Debug.LogError($"[SyncManager] Failed to sync batch: {err}");
                            isSyncing = false;
                        }
                    ));
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
        }
    }
}
