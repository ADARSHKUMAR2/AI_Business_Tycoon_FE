using UnityEngine;
using TMPro;

namespace AIBusinessTycoon.Managers
{
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Inventory Settings")]
        public int maxCarryCapacity = 5;
        public int currentCarrying = 0;

        [Header("Visuals")]
        [SerializeField] private Transform carryPoint; // Above the player's head
        
        private GameObject boxPrefab;
        private TextMeshProUGUI inventoryTextUI;
        private float lastTransferTime = 0f;
        private float transferCooldown = 0.2f; // Transfer 1 item every 0.2 seconds

        private void Start()
        {
            if (carryPoint == null)
            {
                GameObject cp = new GameObject("CarryPoint");
                cp.transform.SetParent(transform);
                cp.transform.localPosition = new Vector3(0, 2.2f, 0.5f); // Above head, slightly forward
                carryPoint = cp.transform;
            }

            boxPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxPrefab.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            boxPrefab.GetComponent<Renderer>().material.color = Color.yellow;
            Destroy(boxPrefab.GetComponent<Collider>());
            boxPrefab.SetActive(false);

            CreateFloatingUI();
        }

        private void CreateFloatingUI()
        {
            GameObject canvasObj = new GameObject("PlayerCanvas");
            canvasObj.transform.SetParent(transform);
            
            // Move it above the player's head
            canvasObj.transform.localPosition = new Vector3(0, 2.5f, 0);
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Scale down the canvas
            canvasObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 100);
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            GameObject textObj = new GameObject("InventoryText");
            textObj.transform.SetParent(canvasObj.transform);
            
            inventoryTextUI = textObj.AddComponent<TextMeshProUGUI>();
            inventoryTextUI.fontSize = 32;
            inventoryTextUI.alignment = TextAlignmentOptions.Center;
            inventoryTextUI.color = Color.yellow;
            inventoryTextUI.fontStyle = FontStyles.Bold;
            
            // Add outline for readability
            inventoryTextUI.outlineWidth = 0.2f;
            inventoryTextUI.outlineColor = new Color(0, 0, 0, 0.8f);
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(200, 100);
            textRect.localPosition = Vector3.zero;
            textRect.localRotation = Quaternion.identity;
            textRect.localScale = Vector3.one;
        }

        // PHYSICAL INTERACTION LOOP
        private void OnTriggerStay(Collider other)
        {
            if (Time.time < lastTransferTime + transferCooldown) return;

            // 1. Check if we are standing in a Supply Zone (truck)
            if (other.CompareTag("SupplyZone"))
            {
                if (currentCarrying < maxCarryCapacity)
                {
                    currentCarrying++;
                    lastTransferTime = Time.time;
                    UpdateVisuals();
                }
            }
            
            // 2. Check if we are standing near an empty Shelf
            InteractableShelf shelf = other.GetComponent<InteractableShelf>();
            if (shelf != null && currentCarrying > 0 && shelf.CanAcceptStock())
            {
                currentCarrying--;
                shelf.AddStock(1);
                lastTransferTime = Time.time;
                UpdateVisuals();
            }
        }

        private void UpdateVisuals()
        {
            inventoryTextUI.text = currentCarrying > 0 ? $"{currentCarrying}/{maxCarryCapacity}" : "";

            foreach (Transform child in carryPoint) Destroy(child.gameObject);

            // Stack boxes visually in player's hands
            for (int i = 0; i < currentCarrying; i++)
            {
                GameObject box = Instantiate(boxPrefab, carryPoint);
                box.SetActive(true);
                box.transform.localPosition = new Vector3(0, i * 0.55f, 0); // Stack upwards
            }
        }
    }
}
