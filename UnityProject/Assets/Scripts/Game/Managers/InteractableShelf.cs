using UnityEngine;
using TMPro;

namespace AIBusinessTycoon.Managers
{
    public class InteractableShelf : MonoBehaviour
    {
        [Header("Shelf Settings")]
        public int maxCapacity = 10;
        public int currentStock = 0;
        
        [Header("Visuals")]
        [SerializeField] private TextMeshProUGUI stockTextUI; // Optional hovering text
        [SerializeField] private Transform itemContainer;     // Where visual boxes will spawn

        private GameObject boxPrefab;

        private void Start()
        {
            // Auto-setup a simple UI if none exists
            if (stockTextUI == null)
            {
                CreateFloatingUI();
            }

            // Simple box prefab to visually show stock
            boxPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxPrefab.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            boxPrefab.GetComponent<Renderer>().material.color = Color.red;
            Destroy(boxPrefab.GetComponent<Collider>());
            boxPrefab.SetActive(false);

            if (itemContainer == null)
            {
                itemContainer = new GameObject("ItemContainer").transform;
                itemContainer.SetParent(transform);
                itemContainer.localPosition = new Vector3(0, 1.2f, 0); // Sit on top of the shelf block
            }

            UpdateVisuals();
        }

        private void CreateFloatingUI()
        {
            GameObject canvasObj = new GameObject("ShelfCanvas");
            canvasObj.transform.SetParent(transform);
            
            // Move it slightly above the shelf
            canvasObj.transform.localPosition = new Vector3(0, 1.5f, 0);
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // CRITICAL FIX: Scale down the entire canvas to fit in the 3D world
            canvasObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 100);
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            GameObject textObj = new GameObject("StockText");
            textObj.transform.SetParent(canvasObj.transform);
            
            stockTextUI = textObj.AddComponent<TextMeshProUGUI>();
            // Use a normal font size now that the canvas is scaled down
            stockTextUI.fontSize = 36; 
            stockTextUI.alignment = TextAlignmentOptions.Center;
            stockTextUI.color = Color.white;
            stockTextUI.fontStyle = FontStyles.Bold;
            
            // Add a slight black outline to make it readable against any background
            stockTextUI.outlineWidth = 0.2f;
            stockTextUI.outlineColor = new Color(0, 0, 0, 0.8f);
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(200, 100);
            textRect.localPosition = Vector3.zero;
            textRect.localRotation = Quaternion.identity;
            textRect.localScale = Vector3.one;
        }

        public bool CanAcceptStock()
        {
            return currentStock < maxCapacity;
        }

        public void AddStock(int amount)
        {
            currentStock = Mathf.Min(currentStock + amount, maxCapacity);
            UpdateVisuals();
        }

        public bool TryTakeStock()
        {
            if (currentStock > 0)
            {
                currentStock--;
                UpdateVisuals();
                return true;
            }
            return false;
        }

        private void UpdateVisuals()
        {
            if (stockTextUI != null)
            {
                stockTextUI.text = $"{currentStock}/{maxCapacity}";
                stockTextUI.color = currentStock == 0 ? Color.red : Color.green;
            }

            // Visual boxes (very simple representation)
            foreach (Transform child in itemContainer)
            {
                Destroy(child.gameObject);
            }

            // Stack boxes
            for (int i = 0; i < currentStock; i++)
            {
                GameObject box = Instantiate(boxPrefab, itemContainer);
                box.SetActive(true);
                // Stack them: 2 per row
                float xOffset = (i % 2 == 0) ? -0.25f : 0.25f;
                float yOffset = (i / 2) * 0.5f; // Adjusted for better stacking
                box.transform.localPosition = new Vector3(xOffset, yOffset, 0);
            }
        }
    }
}
