using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

        private void Awake()
        {
            store = GetComponent<StoreInteractionManager>();
            currentBatch = new TransactionBatchData();
        }

        private void Start()
        {
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
        }

        private IEnumerator SyncRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(syncIntervalSeconds);

                if (currentBatch.total_customers_served > 0 && !isSyncing && GameManager.Instance.CurrentPlayer != null)
                {
                    isSyncing = true;

                    TransactionBatchData batchToSend = new TransactionBatchData
                    {
                        items_sold = new Dictionary<string, int>(currentBatch.items_sold),
                        total_revenue = currentBatch.total_revenue,
                        total_customers_served = currentBatch.total_customers_served
                    };

                    currentBatch = new TransactionBatchData();

                    // 2. Replaced the manual serialization with Newtonsoft!
                    string jsonPayload = JsonConvert.SerializeObject(batchToSend);

                    string playerId = GameManager.Instance.CurrentPlayer.player_id;
                    string businessId = store.BusinessData.business_id;
                    string url = $"{TycoonAPIService.Instance.GetBaseURL()}/api/game/business/{playerId}/{businessId}/sync_transactions";

                    StartCoroutine(TycoonAPIService.Instance.PostRequestRaw(url, jsonPayload,
                        (response) => {
                            isSyncing = false;
                        },
                        (err) => {
                            Debug.LogError($"[SyncManager] Failed to sync batch: {err}");
                            MergeFailedBatch(batchToSend);
                            isSyncing = false;
                        }
                    ));
                }
            }
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
