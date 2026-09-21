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
        [SerializeField] private Transform carryPoint;

        private GameObject boxPrefab;
        private TextMeshProUGUI inventoryTextUI;
        private string heldItemId;

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
            boxPrefab.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
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
            return !string.IsNullOrEmpty(heldItemId);
        }

        public string HeldItemId => heldItemId;

        public void PickUpItem(string itemId)
        {
            if (HasItem())
                return;

            if (currentCarrying >= maxCarryCapacity)
                return;

            heldItemId = itemId;
            currentCarrying = 1;
            UpdateVisuals();
        }

        public string DropHeldItem()
        {
            string item = heldItemId;
            heldItemId = null;
            currentCarrying = 0;
            UpdateVisuals();
            return item;
        }

        private void UpdateVisuals()
        {
            if (inventoryTextUI != null)
            {
                inventoryTextUI.text = HasItem()
                    ? heldItemId
                    : "";
            }

            foreach (Transform child in carryPoint)
            {
                Destroy(child.gameObject);
            }

            if (!HasItem())
                return;

            GameObject box = Instantiate(boxPrefab, carryPoint);
            box.SetActive(true);
            box.transform.localPosition = new Vector3(0, 0, 0);
        }
    }
}
