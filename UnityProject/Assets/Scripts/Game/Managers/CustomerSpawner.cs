using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace AIBusinessTycoon.Managers
{
    public class CustomerSpawner : MonoBehaviour
    {
        public static CustomerSpawner Instance { get; private set; }

        [Header("Spawning Settings")]
        [SerializeField] private GameObject customerPrefab;
        [SerializeField] private float spawnInterval = 5f;
        [SerializeField] private int maxCustomers = 10;

        [Header("Spawn Location")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform exitPoint;

        // ── Object Pooling Variables ──
        private Queue<GameObject> customerPool = new Queue<GameObject>();
        private int activeCustomerCount = 0;
        private bool isSpawning = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (spawnPoint == null)
            {
                GameObject sp = new GameObject("CustomerSpawnPoint");
                sp.transform.position = new Vector3(0, 0.5f, -20f);
                spawnPoint = sp.transform;
            }

            if (customerPrefab == null)
            {
                Debug.LogError("❌ [CustomerSpawner] customerPrefab is NULL!");
                return;
            }

            // ── Pre-warm the Object Pool ──
            for (int i = 0; i < maxCustomers; i++)
            {
                GameObject newCust = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
                newCust.name = "Customer_" + i;
                newCust.transform.SetParent(transform); // Keep the hierarchy clean
                newCust.SetActive(false);               // Hide them immediately
                customerPool.Enqueue(newCust);
            }

            isSpawning = true;
            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            while (isSpawning)
            {
                yield return new WaitForSeconds(spawnInterval);

                if (activeCustomerCount < maxCustomers && GameManager.Instance != null && GameManager.Instance.IsGameReady)
                {
                    if (GameManager.Instance.CurrentPlayer != null && GameManager.Instance.CurrentPlayer.OwnedBusinessCount > 0)
                    {
                        SpawnCustomerFromPool();
                    }
                }
            }
        }
        public Vector3 GetExitPosition() 
        {
            if (exitPoint != null) return exitPoint.position;
            return spawnPoint != null ? spawnPoint.position : transform.position;
        }

        private void SpawnCustomerFromPool()
        {
            if (customerPool.Count > 0)
            {
                GameObject customer = customerPool.Dequeue();
                
                // Safely teleport NavMeshAgent by disabling it first
                var agent = customer.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null) agent.enabled = false;
                
                customer.transform.position = spawnPoint.position;
                
                if (agent != null) agent.enabled = true;
                
                customer.SetActive(true); // Wake them up!
                activeCustomerCount++;
            }
        }

        /// <summary>
        /// Called by the CustomerAI when it reaches the exit.
        /// </summary>
        public void ReturnCustomerToPool(GameObject customer)
        {
            customer.SetActive(false); // Put them back to sleep
            customerPool.Enqueue(customer);
            activeCustomerCount--;
        }
    }
}
