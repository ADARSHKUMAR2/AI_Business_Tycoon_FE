using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace AIBusinessTycoon.Managers
{
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Inventory Settings")]
        public int maxCarryCapacity = 5;
        public int currentCarrying = 0;

        [Header("Visuals")]
        [SerializeField] private Transform carryPoint;

        private GameObject boxPrefab;
        private TextMeshProUGUI inventoryTextUI;
        
        // Changed from single string to a list to support multiple items
        private List<string> heldItems = new List<string>();
        private List<GameObject> visualBoxes = new List<GameObject>();

        private void Start()
        {
            if (carryPoint == null)
            {
                GameObject cp = new GameObject("CarryPoint");
                cp.transform.SetParent(transform);
                cp.transform.localPosition = new Vector3(0, 2.2f, 0.5f);
                carryPoint = cp.transform;
            }

            boxPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxPrefab.name = "BoxTemplate";
            boxPrefab.transform.SetParent(transform);
            boxPrefab.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            boxPrefab.GetComponent<Renderer>().material.color = Color.yellow;
            Destroy(boxPrefab.GetComponent<Collider>());
            boxPrefab.SetActive(false);

            CreateFloatingUI();
            UpdateVisuals();
        }

        private void CreateFloatingUI()
        {
            GameObject canvasObj = new GameObject("PlayerCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, 2.5f, 0);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            canvasObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 100);
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            GameObject textObj = new GameObject("InventoryText");
            textObj.transform.SetParent(canvasObj.transform);

            inventoryTextUI = textObj.AddComponent<TextMeshProUGUI>();
            inventoryTextUI.fontSize = 32;
            inventoryTextUI.alignment = TextAlignmentOptions.Center;
            inventoryTextUI.color = Color.yellow;
            inventoryTextUI.fontStyle = FontStyles.Bold;
            inventoryTextUI.outlineWidth = 0.2f;
            inventoryTextUI.outlineColor = new Color(0, 0, 0, 0.8f);

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(200, 100);
            textRect.localPosition = Vector3.zero;
            textRect.localRotation = Quaternion.identity;
            textRect.localScale = Vector3.one;
        }

        public bool HasItem()
        {
            return heldItems.Count > 0;
        }

        public bool IsFull()
        {
            return currentCarrying >= maxCarryCapacity;
        }

        public int GetAvailableSpace()
        {
            return maxCarryCapacity - currentCarrying;
        }

        // Returns the first item type (for compatibility with existing code)
        public string HeldItemId => heldItems.Count > 0 ? heldItems[0] : null;

        public void PickUpItem(string itemId)
        {
            if (currentCarrying >= maxCarryCapacity)
            {
                Debug.Log($"[PlayerInventory] Cannot pick up {itemId} - inventory full ({currentCarrying}/{maxCarryCapacity})");
                return;
            }

            heldItems.Add(itemId);
            currentCarrying++;
            UpdateVisuals();
            
            Debug.Log($"[PlayerInventory] ✅ Picked up {itemId}. Now carrying: {currentCarrying}/{maxCarryCapacity}");
        }

        public string DropHeldItem()
        {
            if (heldItems.Count == 0)
                return null;

            string item = heldItems[0];
            heldItems.RemoveAt(0);
            currentCarrying--;
            UpdateVisuals();
            
            Debug.Log($"[PlayerInventory] Dropped {item}. Now carrying: {currentCarrying}/{maxCarryCapacity}");
            return item;
        }

        public void DropAllItems()
        {
            int count = heldItems.Count;
            heldItems.Clear();
            currentCarrying = 0;
            UpdateVisuals();
            Debug.Log($"[PlayerInventory] Dropped all {count} items.");
        }

        private void UpdateVisuals()
        {
            // Update text UI
            if (inventoryTextUI != null)
            {
                if (currentCarrying > 0)
                {
                    // Show first item type and count
                    inventoryTextUI.text = $"{heldItems[0]} x{currentCarrying}";
                }
                else
                {
                    inventoryTextUI.text = "";
                }
            }

            // Update stacked boxes visual (like RestockerAI)
            // Create new boxes if needed
            while (visualBoxes.Count < currentCarrying)
            {
                GameObject box = Instantiate(boxPrefab, carryPoint);
                int i = visualBoxes.Count;
                box.transform.localPosition = new Vector3(0, i * 0.45f, 0);
                box.SetActive(true);
                visualBoxes.Add(box);
            }

            // Show/hide boxes based on current count
            for (int i = 0; i < visualBoxes.Count; i++)
            {
                if (i < currentCarrying)
                {
                    visualBoxes[i].SetActive(true);
                }
                else
                {
                    visualBoxes[i].SetActive(false);
                }
            }
        }

        private void LateUpdate()
        {
            // Keep UI facing camera
            if (inventoryTextUI != null && Camera.main != null)
            {
                inventoryTextUI.transform.parent.rotation = Camera.main.transform.rotation;
            }
        }
    }
}
