
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;

namespace AIBusinessTycoon.UI
{
    public class ShelfUpgradeUIManager : MonoBehaviour
    {
        public static ShelfUpgradeUIManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] public GameObject popupPanel;

        [Header("Info")]
        [SerializeField] public TextMeshProUGUI itemNameText;
        [SerializeField] public TextMeshProUGUI capacityText;
        [SerializeField] public TextMeshProUGUI costText;

        [Header("Controls")]
        [SerializeField] public Button upgradeButton;
        [SerializeField] public Button closeButton;

        private Managers.InteractableShelf currentShelf;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(CloseUI);
            if (upgradeButton != null) upgradeButton.onClick.AddListener(RequestUpgrade);

            popupPanel?.SetActive(false);
        }

        public void OpenUpgradeUI(Managers.InteractableShelf shelf)
        {
            currentShelf = shelf;
            RefreshUI();
            popupPanel?.SetActive(true);
            Managers.PlayerController.Instance?.SetJoystickInput(Vector2.zero);
        }

        public void CloseUI()
        {
            popupPanel?.SetActive(false);
            currentShelf = null;
        }

        private void RefreshUI()
        {
            if (currentShelf == null || currentShelf.itemData == null) return;

            var gm = Managers.GameManager.Instance;
            float money = gm?.CurrentPlayer?.money ?? 0f;

            if (itemNameText != null) itemNameText.text = currentShelf.itemData.name;

            int currentCap = currentShelf.maxCapacity;
            if (currentCap >= 30)
            {
                if (capacityText != null) capacityText.text = "Max Capacity Reached (30)";
                if (costText != null) costText.text = "MAX";
                if (upgradeButton != null) upgradeButton.interactable = false;
            }
            else
            {
                int nextCap = currentCap == 10 ? 20 : 30;
                float cost = nextCap == 20 ? 2000f : 5000f; // Matches Backend constants.py

                if (capacityText != null) capacityText.text = $"Upgrade Capacity: {currentCap} ➔ {nextCap}";
                if (costText != null) costText.text = $"Rs. {cost:N0}";
                
                if (upgradeButton != null) upgradeButton.interactable = (money >= cost);
            }
        }

        private void RequestUpgrade()
        {
            if (currentShelf == null) return;
            var gm = Managers.GameManager.Instance;

            int nextCap = currentShelf.maxCapacity == 10 ? 20 : 30;
            float cost = nextCap == 20 ? 2000f : 5000f;

            upgradeButton.interactable = false;
            var request = new ShelfUpgradeRequest(currentShelf.itemKey, nextCap);

            TycoonAPIService.Instance.UpgradeShelf(
                currentShelf.playerId, currentShelf.businessId, request,
                (updatedBusiness) => 
                {
                    Debug.Log($"[ShelfUpgradeUI] Successfully upgraded {currentShelf.itemKey} to {nextCap}");
                    
                    // Deduct money locally for UI
                    gm.CurrentPlayer.money -= cost;
                    HUDManager.Instance?.UpdateMoney(gm.CurrentPlayer.money);

                    // Visually expand the shelf in the 3D world
                    currentShelf.maxCapacity = nextCap;
                    currentShelf.itemData.max_stock = nextCap;
                    currentShelf.UpdateVisuals();

                    HUDManager.Instance?.ShowNotification("📦 Shelf Upgraded!", 2f);
                    RefreshUI();
                },
                (err) => 
                {
                    Debug.LogWarning($"[ShelfUpgradeUI] Failed to upgrade: {err}");
                    HUDManager.Instance?.ShowNotification("❌ Upgrade Failed!", 2f);
                    RefreshUI(); 
                }
            );
        }
    }
}
