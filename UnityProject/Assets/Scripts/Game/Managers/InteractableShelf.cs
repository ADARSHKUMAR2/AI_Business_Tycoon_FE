using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
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

        // We no longer instantiate at runtime. We just cache the children of itemContainer!
        private List<GameObject> pooledBoxes = new List<GameObject>();

        private void Start()
        {
            // Warn individually but NEVER return early — shelf must function even without visuals
            if (stockTextUI == null)
                Debug.LogWarning($"[InteractableShelf] stockTextUI missing on {gameObject.name}. Run the Editor Setup script.");
            if (itemNameTextUI == null)
                Debug.LogWarning($"[InteractableShelf] itemNameTextUI missing on {gameObject.name}. Run the Editor Setup script.");

            // Cache all pre-built child objects inside itemContainer into the pool
            if (itemContainer != null)
            {
                foreach (Transform child in itemContainer)
                {
                    pooledBoxes.Add(child.gameObject);
                    child.gameObject.SetActive(false); // Hide all until stock is known
                }
            }
            else
            {
                Debug.LogWarning($"[InteractableShelf] itemContainer missing on {gameObject.name}. Visual boxes will not show.");
            }

            // NOTE: Do NOT cap maxCapacity here.
            // InitializeFromBackend() will set the real values from backend data.
            // pooledBoxes.Count is only a VISUAL limit, not a functional one.
            UpdateVisuals();
        }

        public void InitializeFromBackend(string key, InventoryItem data, string pId, string bId)
        {
            itemKey = key;
            itemData = data;
            playerId = pId;
            businessId = bId;

            // Use real backend capacity for game logic — visual limit is handled in UpdateVisuals()
            maxCapacity = data.max_stock;
            currentStock = Mathf.Min(data.stock, maxCapacity);

            if (itemNameTextUI != null) itemNameTextUI.text = data.name;

            UpdateVisuals();
        }

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
            currentStock = Mathf.Clamp(currentStock + amount, 0, maxCapacity);
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
            // Failsafe bounds
            if (currentStock > maxCapacity) currentStock = maxCapacity;
            if (currentStock < 0) currentStock = 0;

            if (stockTextUI != null)
            {
                stockTextUI.text = $"{currentStock}/{maxCapacity}";
                stockTextUI.color = currentStock == 0 ? Color.red : Color.green;
            }

            if (pooledBoxes.Count == 0) return;

            // Visual boxes are capped to the pool size.
            // Game logic (CanAcceptStock etc.) always uses the real maxCapacity from backend.
            int visualStock = Mathf.Min(currentStock, pooledBoxes.Count);
            for (int i = 0; i < pooledBoxes.Count; i++)
            {
                pooledBoxes[i].SetActive(i < visualStock);
            }
        }
    }
}
