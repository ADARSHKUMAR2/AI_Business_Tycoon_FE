using UnityEngine;
using System.Collections.Generic;
using AIBusinessTycoon.Data;
using TMPro;

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

        [Header("Delivery - Multi-Zone System")]
        [SerializeField] private Transform supplyZoneParent; // Parent object that will hold all supply zones
        
        private Dictionary<string, GameObject> supplyZonesByItem = new Dictionary<string, GameObject>();
        private bool isHovered = false;
        private bool employeesSpawned = false;

        public DeliveryStatusResponse CurrentDeliveryStatus { get; private set; }

        /// <summary>
        /// Apply delivery status and show/hide supply zones accordingly
        /// </summary>
        public void ApplyDeliveryStatus(DeliveryStatusResponse status)
        {
            CurrentDeliveryStatus = status;

            if (supplyZoneParent == null)
            {
                CreateSupplyZoneParent();
            }

            // Hide all existing zones first
            foreach (var zone in supplyZonesByItem.Values)
            {
                if (zone != null)
                    zone.SetActive(false);
            }

            if (status == null || !status.supply_available)
            {
                Debug.Log($"[StoreInteractionManager] Supply not available for {BusinessData?.business_id}");
                return;
            }

            // Show zones only for items that have stock in delivery
            if (BusinessData != null && BusinessData.inventory != null)
            {
                int activeZones = 0;
                foreach (var item in BusinessData.inventory)
                {
                    if (item.Value != null && item.Value.stock > 0)
                    {
                        GameObject zone = GetOrCreateSupplyZone(item.Key, item.Value.name);
                        zone.SetActive(true);
                        activeZones++;
                        Debug.Log($"[StoreInteractionManager] Activated supply zone for: {item.Key} (stock: {item.Value.stock})");
                    }
                }
                Debug.Log($"[StoreInteractionManager] Total active supply zones: {activeZones}");
            }
        }

        private void CreateSupplyZoneParent()
        {
            GameObject parent = new GameObject("SupplyZones");
            parent.transform.SetParent(transform);
            parent.transform.localPosition = new Vector3(0, 0, -5f); // Behind the store
            supplyZoneParent = parent.transform;
        }

        /// <summary>
        /// Get or create a supply zone for a specific item
        /// </summary>
        private GameObject GetOrCreateSupplyZone(string itemKey, string itemDisplayName)
        {
            if (supplyZonesByItem.ContainsKey(itemKey))
                return supplyZonesByItem[itemKey];

            // Create new supply zone GameObject
            GameObject zone = new GameObject($"SupplyZone_{itemKey}");
            zone.transform.SetParent(supplyZoneParent);
            
            // Position zones in a horizontal row
            int index = supplyZonesByItem.Count;
            zone.transform.localPosition = new Vector3(index * 3f - 3f, 0.1f, 0);
            zone.transform.localRotation = Quaternion.identity;
            
            // Create visual platform
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Platform";
            visual.transform.SetParent(zone.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(2.5f, 0.2f, 2.5f);
            
            // Color-code by item type
            Renderer renderer = visual.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = GetColorForItem(itemKey);
            
            // Remove collider from visual (we'll add trigger to parent)
            Destroy(visual.GetComponent<Collider>());
            
            // Add trigger collider to the zone parent
            BoxCollider trigger = zone.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0, 1f, 0);
            trigger.size = new Vector3(2.5f, 3f, 2.5f);
            
            // Add a tag for identification
            zone.tag = $"SupplyZone_{itemKey}";
            
            // Create floating label
            CreateSupplyZoneLabel(zone, itemDisplayName, itemKey);
            
            // Store reference
            supplyZonesByItem[itemKey] = zone;
            
            Debug.Log($"[StoreInteractionManager] Created supply zone for: {itemKey} at position {zone.transform.position}");
            return zone;
        }

        private void CreateSupplyZoneLabel(GameObject zone, string displayName, string itemKey)
        {
            GameObject canvasObj = new GameObject("Label");
            canvasObj.transform.SetParent(zone.transform);
            canvasObj.transform.localPosition = new Vector3(0, 2f, 0);
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(300, 150);
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            // Item name text
            GameObject textObj = new GameObject("ItemName");
            textObj.transform.SetParent(canvasObj.transform);
            
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = $"📦 {displayName}";
            text.fontSize = 48;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.color = Color.white;
            text.outlineWidth = 0.3f;
            text.outlineColor = new Color(0, 0, 0, 0.9f);
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(300, 150);
            textRect.localPosition = Vector3.zero;
            textRect.localRotation = Quaternion.identity;
            textRect.localScale = Vector3.one;
        }

        private Color GetColorForItem(string itemKey)
        {
            // Assign colors based on item type
            string lower = itemKey.ToLower();
            
            if (lower.Contains("tomato")) return new Color(0.9f, 0.2f, 0.2f); // Red
            if (lower.Contains("rice")) return new Color(0.95f, 0.95f, 0.85f); // Beige
            if (lower.Contains("fe") || lower.Contains("iron")) return new Color(0.6f, 0.6f, 0.6f); // Gray
            if (lower.Contains("wheat")) return new Color(0.9f, 0.7f, 0.3f); // Golden
            if (lower.Contains("milk")) return new Color(0.95f, 0.95f, 1f); // White-blue
            if (lower.Contains("bread")) return new Color(0.8f, 0.6f, 0.4f); // Brown
            if (lower.Contains("egg")) return new Color(1f, 0.95f, 0.8f); // Cream
            
            // Default: cycle through colors based on hash
            int hash = itemKey.GetHashCode();
            float hue = (hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.7f, 0.9f);
        }

        /// <summary>
        /// Try to supply the player with a specific item from its zone
        /// </summary>
        public void TryPlayerSupply(PlayerController player, string itemKey)
        {
            if (player == null)
            {
                Debug.LogWarning($"{name}: Tried to use supply with a null player.");
                return;
            }

            if (CurrentDeliveryStatus == null || !CurrentDeliveryStatus.supply_available)
            {
                Debug.Log($"{name}: Supply is not available for store {BusinessData?.business_id}");
                return;
            }

            if (player.Inventory == null)
            {
                Debug.LogWarning($"{name}: Player inventory is missing on {player.name}.");
                return;
            }

            if (player.Inventory.IsFull())
            {
                Debug.Log($"{name}: Player inventory is full ({player.Inventory.currentCarrying}/{player.Inventory.maxCarryCapacity})");
                return;
            }

            // Check if this specific item has stock in delivery
            if (BusinessData == null || 
                !BusinessData.inventory.ContainsKey(itemKey) ||
                BusinessData.inventory[itemKey] == null ||
                BusinessData.inventory[itemKey].stock <= 0)
            {
                Debug.Log($"{name}: No stock available for {itemKey} in delivery");
                return;
            }

            // Give item to player
            player.Inventory.PickUpItem(itemKey);
            Debug.Log($"✅ [StoreInteractionManager] Player picked up '{itemKey}' from supply zone");
        }

        public bool IsSupplyUsable()
        {
            return CurrentDeliveryStatus != null && CurrentDeliveryStatus.supply_available;
        }

        /// <summary>
        /// Get the supply zone GameObject for a specific item (for RestockerAI)
        /// </summary>
        public GameObject GetSupplyZoneForItem(string itemKey)
        {
            if (supplyZonesByItem.ContainsKey(itemKey))
                return supplyZonesByItem[itemKey];
            return null;
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
                        else if (emp.role == "restocker")
                        {
                            RestockerAI restockerAI = ai.GetComponent<RestockerAI>();
                            if (restockerAI != null)
                                restockerAI.BindToStore(this);
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
