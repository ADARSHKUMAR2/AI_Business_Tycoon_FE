using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.Managers
{
    public class InteractableShelf : MonoBehaviour
    {
        [Header("Backend Data")]
        public string itemKey;
        public string playerId;
        public string businessId;
        public InventoryItem itemData;

        [Header("Shelf Settings")]
        public int maxCapacity = 10;
        public int currentStock = 0;
        
        [Header("Visuals")]
        [SerializeField] private TextMeshProUGUI stockTextUI; 
        [SerializeField] private TextMeshProUGUI itemNameTextUI; 
        [SerializeField] private Transform itemContainer;     

        private GameObject boxPrefab;

        private void Start()
        {
            if (stockTextUI == null) CreateFloatingUI();

            boxPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxPrefab.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            boxPrefab.GetComponent<Renderer>().material.color = new Color(0.8f, 0.3f, 0.2f); // Terracotta box
            Destroy(boxPrefab.GetComponent<Collider>());
            boxPrefab.SetActive(false);

            if (itemContainer == null)
            {
                itemContainer = new GameObject("ItemContainer").transform;
                itemContainer.SetParent(transform);
                itemContainer.localPosition = new Vector3(0, 1.2f, 0); 
            }

            // Ensure the shelf has a collider so we can click it!
            if (GetComponent<Collider>() == null)
            {
                var col = gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0, 0.5f, 0);
                col.size = new Vector3(1.5f, 1.5f, 0.5f);
            }

            UpdateVisuals();
        }

        public void InitializeFromBackend(string key, InventoryItem data, string pId, string bId)
        {
            itemKey = key;
            itemData = data;
            playerId = pId;
            businessId = bId;

            maxCapacity = data.max_stock;
            currentStock = Mathf.Min(data.stock, maxCapacity);

            if (itemNameTextUI != null) itemNameTextUI.text = data.name;

            UpdateVisuals();
        }

        // ── Click to open Upgrade UI ──
        private void OnMouseDown()
        {
            if (CameraController.Instance == null || CameraController.Instance.CurrentMode != CameraController.CameraMode.MicroView) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (itemData != null)
            {
                UI.ShelfUpgradeUIManager.Instance?.OpenUpgradeUI(this);
            }
        }

        public bool CanAcceptStock() => currentStock < maxCapacity;

        public void AddStock(int amount)
        {
            currentStock = Mathf.Min(currentStock + amount, maxCapacity);
            if (itemData != null) itemData.stock = currentStock;
            UpdateVisuals();
        }

        public bool TryTakeStock()
        {
            if (currentStock > 0)
            {
                currentStock--;
                if (itemData != null) itemData.stock = currentStock;
                UpdateVisuals();
                return true;
            }
            return false;
        }

        public void UpdateVisuals()
        {
            if (stockTextUI != null)
            {
                stockTextUI.text = $"{currentStock}/{maxCapacity}";
                stockTextUI.color = currentStock == 0 ? Color.red : Color.green;
            }

            if (itemContainer == null) return;

            foreach (Transform child in itemContainer) Destroy(child.gameObject);

            // Stack boxes: 3 per row to fit up to 30!
            for (int i = 0; i < currentStock; i++)
            {
                GameObject box = Instantiate(boxPrefab, itemContainer);
                box.SetActive(true);
                
                float xOffset = -0.4f + (i % 3) * 0.4f; 
                float yOffset = (i / 3) * 0.4f; 
                box.transform.localPosition = new Vector3(xOffset, yOffset, 0);
            }
        }

        private void CreateFloatingUI()
        {
            GameObject canvasObj = new GameObject("ShelfCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, 1.5f, 0);
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 150);
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            // Name Text
            GameObject nameObj = new GameObject("ItemNameText");
            nameObj.transform.SetParent(canvasObj.transform);
            itemNameTextUI = nameObj.AddComponent<TextMeshProUGUI>();
            itemNameTextUI.fontSize = 28; 
            itemNameTextUI.alignment = TextAlignmentOptions.Center;
            itemNameTextUI.color = new Color(1f, 0.8f, 0.2f);
            itemNameTextUI.outlineWidth = 0.2f;
            itemNameTextUI.outlineColor = new Color(0, 0, 0, 0.8f);
            RectTransform nameRect = nameObj.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(300, 50);
            nameRect.localPosition = new Vector3(0, 40, 0);
            nameRect.localRotation = Quaternion.identity;
            nameRect.localScale = Vector3.one;

            // Stock Text
            GameObject textObj = new GameObject("StockText");
            textObj.transform.SetParent(canvasObj.transform);
            stockTextUI = textObj.AddComponent<TextMeshProUGUI>();
            stockTextUI.fontSize = 36; 
            stockTextUI.alignment = TextAlignmentOptions.Center;
            stockTextUI.fontStyle = FontStyles.Bold;
            stockTextUI.outlineWidth = 0.2f;
            stockTextUI.outlineColor = new Color(0, 0, 0, 0.8f);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(300, 100);
            textRect.localPosition = new Vector3(0, -10, 0);
            textRect.localRotation = Quaternion.identity;
            textRect.localScale = Vector3.one;
        }
    }
}
