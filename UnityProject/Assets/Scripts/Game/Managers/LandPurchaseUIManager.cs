using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;

namespace AIBusinessTycoon.UI
{
    /// <summary>
    /// Manages the Land Purchase popup UI.
    /// Shown when the player clicks an adjacent unowned tile.
    /// </summary>
    public class LandPurchaseUIManager : MonoBehaviour
    {
        public static LandPurchaseUIManager Instance { get; private set; }
        
        [Header("UI References")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI positionText;
        [SerializeField] private TextMeshProUGUI playerMoneyText;
        [SerializeField] private TextMeshProUGUI affordabilityText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI confirmButtonText;
        
        // State
        private Position pendingPosition;
        private float pendingCost;
        
        // References
        private Managers.GameManager gameManager;
        private Managers.GridManager gridManager;
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        private void Start()
        {
            gameManager = Managers.GameManager.Instance;
            gridManager = Managers.GridManager.Instance;
            
            // Subscribe to tile click events from GridManager
            if (gridManager != null)
                gridManager.OnTileClickedForPurchase += OnTileClicked;
            
            // Wire up buttons
            if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClicked);
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
            
            // Hide popup initially
            HidePopup();
        }
        
        private void OnDestroy()
        {
            if (gridManager != null)
                gridManager.OnTileClickedForPurchase -= OnTileClicked;
        }
        
        #region Popup Control
        
        private void OnTileClicked(Position position, float cost)
        {
            // Don't show if currently placing a building
            if (Managers.BuildingPlacementManager.Instance != null && 
                Managers.BuildingPlacementManager.Instance.IsPlacing)
                return;
            
            pendingPosition = position;
            pendingCost = cost;
            
            ShowPopup(position, cost);
        }
        
        private void ShowPopup(Position position, float cost)
        {
            if (popupPanel == null) return;
            
            float playerMoney = gameManager?.CurrentPlayer?.money ?? 0f;
            bool canAfford = playerMoney >= cost;
            
            // Update UI text
            if (titleText != null)
                titleText.text = "Purchase Land?";
            
            if (costText != null)
                costText.text = $"Rs.{cost:N0}";
            
            if (positionText != null)
                positionText.text = $"Location: ({position.x}, {position.y})";
            
            if (playerMoneyText != null)
                playerMoneyText.text = $"Your Balance: Rs.{playerMoney:N0}";
            
            if (affordabilityText != null)
            {
                if (canAfford)
                {
                    affordabilityText.text = $"Remaining: Rs.{playerMoney - cost:N0}";
                    affordabilityText.color = Color.green;
                }
                else
                {
                    affordabilityText.text = $"Need Rs.{cost - playerMoney:N0} more!";
                    affordabilityText.color = Color.red;
                }
            }
            
            // Enable/disable confirm button based on affordability
            if (confirmButton != null)
                confirmButton.interactable = canAfford;
            
            if (confirmButtonText != null)
                confirmButtonText.text = canAfford ? "Buy Land" : "Too Expensive";
            
            popupPanel.SetActive(true);
        }
        
        private void HidePopup()
        {
            if (popupPanel != null)
                popupPanel.SetActive(false);
            
            pendingPosition = null;
            pendingCost = 0f;
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnConfirmClicked()
        {
            if (pendingPosition == null || gameManager == null) return;
            
            Debug.Log($"[LandPurchaseUIManager] Purchasing land at ({pendingPosition.x}, {pendingPosition.y}) for Rs.{pendingCost}");
            
            // Hide popup immediately for snappy UX
            Position positionToPurchase = pendingPosition;
            float costToPurchase = pendingCost;
            HidePopup();
            
            // Optimistic money deduction
            gameManager.DeductMoneyLocal(costToPurchase);
            
            // Call API
            LandPurchaseRequest request = new LandPurchaseRequest(
                gameManager.CurrentPlayer.player_id,
                positionToPurchase
            );
            
            TycoonAPIService.Instance.PurchaseLand(
                request,
                OnPurchaseSuccess,
                OnPurchaseError
            );
        }
        
        private void OnCancelClicked()
        {
            Debug.Log("[LandPurchaseUIManager] Land purchase cancelled.");
            HidePopup();
        }
        
        #endregion
        
        #region API Callbacks
        
        private void OnPurchaseSuccess(LandTile tile)
        {
            Debug.Log($"[LandPurchaseUIManager] Land purchased! ({tile.position.x}, {tile.position.y})");
            
            // Update grid - this also refreshes purchasable tile indicators
            if (gridManager != null)
                gridManager.OnLandPurchaseSuccess(tile);
            
            // Refresh player data to sync stats
            gameManager.RefreshPlayerData();
            
            // Show notification
            HUDManager.Instance?.ShowNotification($"Land purchased at ({tile.position.x}, {tile.position.y})!", 2f);
        }
        
        private void OnPurchaseError(string error)
        {
            Debug.LogError($"[LandPurchaseUIManager] Purchase failed: {error}");
            
            // Refund optimistic deduction
            gameManager.RefreshPlayerData();
            
            // Show error notification
            HUDManager.Instance?.ShowNotification("Purchase failed! Check your balance.", 3f);
        }
        
        #endregion
    }
}
