using UnityEngine;
using System.Collections.Generic;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.Managers
{
    public class StoreInteractionManager : MonoBehaviour
    {
        [Header("Store Data")]
        public BusinessData BusinessData;

        [Header("Entrance")]
        [SerializeField] private Transform entrancePoint;

        [Header("Interaction Prompt")]
        [SerializeField] private GameObject interactionPromptUI;

        [Header("Delivery")]
        [SerializeField] private GameObject supplyZone;
        public GameObject SupplyZone => supplyZone;

        private bool isHovered = false;
        private bool employeesSpawned = false;

        public DeliveryStatusResponse CurrentDeliveryStatus { get; private set; }

        public void ApplyDeliveryStatus(DeliveryStatusResponse status)
        {
            CurrentDeliveryStatus = status;

            if (supplyZone != null)
            {
                if (status == null)
                {
                    supplyZone.SetActive(false);
                    return;
                }

                supplyZone.SetActive(status.supply_available);
            }
        }

        public bool IsSupplyUsable()
        {
            return CurrentDeliveryStatus != null && CurrentDeliveryStatus.supply_available;
        }

        public void TryPlayerSupply(PlayerController player, bool skipPositionCheck = false)
        {
            if (player == null)
            {
                Debug.LogWarning($"{name}: Tried to use supply with a null player.");
                return;
            }

            if (CurrentDeliveryStatus == null)
            {
                Debug.Log($"{name}: No delivery status available for this store.");
                return;
            }

            if (!CurrentDeliveryStatus.supply_available)
            {
                Debug.Log($"{name}: Supply is not available for store {BusinessData?.business_id}");
                return;
            }

            if (supplyZone == null)
            {
                Debug.LogWarning($"{name}: Supply zone is missing.");
                return;
            }

            if (player.Inventory == null)
            {
                Debug.LogWarning($"{name}: Player inventory is missing on {player.name}.");
                return;
            }

            // ✅ CHANGED: Check if inventory is FULL, not just if it has any item
            if (player.Inventory.IsFull())
            {
                Debug.Log($"{name}: Player inventory is full ({player.Inventory.currentCarrying}/{player.Inventory.maxCarryCapacity}) and cannot pick up more supplies.");
                return;
            }

            string itemKey = GetNextSupplyItem();
            if (string.IsNullOrEmpty(itemKey))
            {
                Debug.Log($"{name}: No supply item is available for store {BusinessData?.business_id}");
                return;
            }

            player.Inventory.PickUpItem(itemKey);
            Debug.Log($"✅ [StoreInteractionManager] Player picked up '{itemKey}' from supply zone at {BusinessData?.business_id}");
        }


        private string GetNextSupplyItem()
        {
            if (BusinessData == null || BusinessData.inventory == null || BusinessData.inventory.Count == 0)
                return string.Empty;

            foreach (var item in BusinessData.inventory)
            {
                if (item.Value != null && item.Value.stock > 0)
                    return item.Key;
            }

            return string.Empty;
        }

        private void Awake()
        {
            if (entrancePoint == null)
            {
                GameObject ep = new GameObject("EntrancePoint");
                ep.transform.SetParent(transform);
                ep.transform.localPosition = new Vector3(0, 0.1f, -3f);
                entrancePoint = ep.transform;
            }

            HidePrompt();
        }

        public void SpawnSavedEmployees(GameObject cashierPrefab, GameObject restockerPrefab, GameObject cleanerPrefab)
        {
            if (employeesSpawned || BusinessData == null || BusinessData.employees == null) return;

            if (BusinessData.inventory != null)
            {
                var shelves = GetComponentsInChildren<InteractableShelf>();

                var activeItems = new List<KeyValuePair<string, InventoryItem>>(BusinessData.inventory);

                for (int i = 0; i < shelves.Length && i < activeItems.Count; i++)
                {
                    shelves[i].InitializeFromBackend(
                        activeItems[i].Key,
                        activeItems[i].Value,
                        BusinessData.player_id,
                        BusinessData.business_id
                    );
                }
            }

            if (BusinessData.employees != null)
            {
                foreach (Employee emp in BusinessData.employees)
                {
                    GameObject prefab = null;
                    Vector3 spawnOffset = Vector3.zero;

                    if (emp.role == "cashier")
                    {
                        prefab = cashierPrefab;
                        spawnOffset = new Vector3(2f, 0.5f, 0f);
                    }
                    else if (emp.role == "restocker")
                    {
                        prefab = restockerPrefab;
                        spawnOffset = new Vector3(-2f, 0.5f, 0f);
                    }
                    else if (emp.role == "cleaner")
                    {
                        prefab = cleanerPrefab;
                        spawnOffset = new Vector3(0f, 0.5f, 2f);
                    }

                    if (prefab != null)
                    {
                        Vector3 spawnPos = GetEntrancePosition() + spawnOffset;
                        GameObject ai = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
                        ai.name = $"{emp.role}_{emp.employee_id.Substring(0, 4)}";

                        if (emp.role == "cleaner")
                        {
                            CleanerAI cleanerAI = ai.GetComponent<CleanerAI>();
                            if (cleanerAI != null)
                                cleanerAI.Initialize(BusinessData.player_id, BusinessData.business_id);
                        }

                        var interactionManager = ai.GetComponent<EmployeeInteractionManager>();
                        if (interactionManager != null)
                        {
                            interactionManager.Initialize(emp, BusinessData.business_id);
                        }

                        Debug.Log($"[StoreInteractionManager] Respawned saved {emp.role}");
                    }
                }
            }

            employeesSpawned = true;
        }

        private void OnMouseEnter()
        {
            if (GameManager.Instance == null || CameraController.Instance == null) return;
            if (CameraController.Instance.CurrentMode != CameraController.CameraMode.MacroView) return;

            isHovered = true;
            ShowPrompt();
        }

        private void OnMouseExit()
        {
            isHovered = false;
            HidePrompt();
        }

        private void OnMouseDown()
        {
            if (GameManager.Instance == null || CameraController.Instance == null) return;
            if (CameraController.Instance.CurrentMode != CameraController.CameraMode.MacroView) return;

            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            GameManager.Instance.EnterStore(this);
        }

        public Vector3 GetEntrancePosition() => entrancePoint.position;

        private void ShowPrompt()
        {
            if (interactionPromptUI != null) interactionPromptUI.SetActive(true);
        }

        private void HidePrompt()
        {
            if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
        }
    }
}
