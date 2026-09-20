using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;
using System.Linq;

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

        // CustomerAI calls this locally when they successfully checkout
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
                    
                    // Copy and clear the batch so we don't lose new sales while syncing
                    TransactionBatchData batchToSend = new TransactionBatchData
                    {
                        items_sold = new Dictionary<string, int>(currentBatch.items_sold),
                        total_revenue = currentBatch.total_revenue,
                        total_customers_served = currentBatch.total_customers_served
                    };
                    
                    currentBatch = new TransactionBatchData();

                    // Convert dict to JSON string format manually or using a helper if Unity's JsonUtility struggles with Dictionaries
                    string jsonPayload = SerializeBatch(batchToSend);

                    string playerId = GameManager.Instance.CurrentPlayer.player_id;
                    string businessId = store.BusinessData.business_id;
                    string url = $"{TycoonAPIService.Instance.GetBaseURL()}/api/game/business/{playerId}/{businessId}/sync_transactions";

                    StartCoroutine(TycoonAPIService.Instance.PostRequestRaw(url, jsonPayload, 
                        (response) => {
                            isSyncing = false;
                            // Optionally update local store data with verified BE response
                        },
                        (err) => {
                            Debug.LogError($"[SyncManager] Failed to sync batch: {err}");
                            // On failure, merge the failed batch back into currentBatch so we don't lose money
                            MergeFailedBatch(batchToSend);
                            isSyncing = false;
                        }
                    ));
                }
            }
        }

        // Helper to serialize Dictionary for Unity's JsonUtility
        private string SerializeBatch(TransactionBatchData batch)
        {
            string itemsJson = string.Join(",", batch.items_sold.Select(kv => $"\"{kv.Key}\":{kv.Value}"));
            return $"{{\"items_sold\": {{{itemsJson}}}, \"total_revenue\": {batch.total_revenue}, \"total_customers_served\": {batch.total_customers_served}}}";
        }
        
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
