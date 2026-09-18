using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using AIBusinessTycoon.Data;

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

        private List<GameObject> activeCustomers = new List<GameObject>();
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
                sp.transform.position = new Vector3(0, 0, -20); // Spawns out on the "street"
                spawnPoint = sp.transform;
            }

            // Start spawning logic
            isSpawning = true;
            StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            while (isSpawning)
            {
                yield return new WaitForSeconds(spawnInterval);

                // Only spawn if we haven't hit the limit and the player is actually playing
                if (activeCustomers.Count < maxCustomers && GameManager.Instance != null && GameManager.Instance.IsGameReady)
                {
                    // Check if player has at least one open store to visit
                    if (GameManager.Instance.CurrentPlayer != null && GameManager.Instance.CurrentPlayer.OwnedBusinessCount > 0)
                    {
                        SpawnCustomer();
                    }
                }
            }
        }

        private void SpawnCustomer()
        {
            if (customerPrefab == null || spawnPoint == null)
            {
                Debug.LogWarning("[CustomerSpawner] Missing Prefab or SpawnPoint!");
                return;
            }

            // Spawn the customer at the spawn point
            GameObject customer = Instantiate(customerPrefab, spawnPoint.position, Quaternion.identity);
            customer.name = "Customer_" + Random.Range(1000, 9999);
            customer.SetActive(true);
            
            activeCustomers.Add(customer);
        }

        public void RemoveCustomer(GameObject customer)
        {
            activeCustomers.Remove(customer);
            Destroy(customer);
        }
    }
}
