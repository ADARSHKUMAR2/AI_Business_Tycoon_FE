using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AIBusinessTycoon.UI
{
    /// <summary>
    /// Manages the building selection menu.
    /// Shows available buildings, costs, and handles selection.
    /// </summary>
    public class BuildMenuManager : MonoBehaviour
    {
        public static BuildMenuManager Instance { get; private set; }
        
        [Header("UI References")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private Button closeButton;
        
        [Header("Building Buttons")]
        [SerializeField] private Button kiranaButton;
        [SerializeField] private Button pizzaButton;
        [SerializeField] private Button cafeButton;
        
        [Header("Building Info")]
        [SerializeField] private TextMeshProUGUI kiranaInfoText;
        [SerializeField] private TextMeshProUGUI pizzaInfoText;
        [SerializeField] private TextMeshProUGUI cafeInfoText;
        
        [Header("Building Prefabs")]
        [SerializeField] private GameObject kiranaPrefab;
        [SerializeField] private GameObject pizzaPrefab;
        [SerializeField] private GameObject cafePrefab;
        
        [Header("Building Costs")]
        [SerializeField] private float kiranaCost = 5000f;
        [SerializeField] private float pizzaCost = 15000f;
        [SerializeField] private float cafeCost = 25000f;
        
        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.B;
        
        private Managers.GameManager gameManager;
        private Managers.BuildingPlacementManager placementManager;
        private bool isMenuOpen = false;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void Start()
        {
            gameManager = Managers.GameManager.Instance;
            placementManager = Managers.BuildingPlacementManager.Instance;
            
            if (gameManager == null)
            {
                Debug.LogError("[BuildMenuManager] GameManager not found!");
            }
            
            if (placementManager == null)
            {
                Debug.LogError("[BuildMenuManager] BuildingPlacementManager not found!");
            }
            
            // Setup buttons
            SetupButtons();
            
            // Update building info
            UpdateBuildingInfo();
            
            // Hide menu initially
            HideMenu();
        }
        
        private void Update()
        {
            // Toggle menu with B key (or Tab)
            if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleMenu();
            }
            
            // Close menu when placement starts
            if (placementManager != null && placementManager.IsPlacing && isMenuOpen)
            {
                HideMenu();
            }
        }
        
        #region Menu Control
        
        public void ShowMenu()
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
                isMenuOpen = true;
                UpdateButtonStates();
                Debug.Log("[BuildMenuManager] Menu opened");
            }
        }
        
        public void HideMenu()
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
                isMenuOpen = false;
                Debug.Log("[BuildMenuManager] Menu closed");
            }
        }
        
        public void ToggleMenu()
        {
            if (isMenuOpen)
                HideMenu();
            else
                ShowMenu();
        }
        
        #endregion
        
        #region Button Setup
        
        private void SetupButtons()
        {
            if (kiranaButton != null)
                kiranaButton.onClick.AddListener(OnKiranaButtonClicked);
            
            if (pizzaButton != null)
                pizzaButton.onClick.AddListener(OnPizzaButtonClicked);
            
            if (cafeButton != null)
                cafeButton.onClick.AddListener(OnCafeButtonClicked);
            
            if (closeButton != null)
                closeButton.onClick.AddListener(HideMenu);
        }
        
        private void UpdateBuildingInfo()
        {
            if (kiranaInfoText != null)
                kiranaInfoText.text = $"Kirana Store\n₹{kiranaCost:N0}\n\nBasic grocery shop\nfor daily needs";
            
            if (pizzaInfoText != null)
                pizzaInfoText.text = $"Pizza Outlet\n₹{pizzaCost:N0}\n\nFast food restaurant\nwith delivery";
            
            if (cafeInfoText != null)
                cafeInfoText.text = $"Cafe\n₹{cafeCost:N0}\n\nCozy coffee shop\nwith seating";
        }
        
        private void UpdateButtonStates()
        {
            if (gameManager == null || gameManager.CurrentPlayer == null) return;
            
            float playerMoney = gameManager.CurrentPlayer.money;
            
            // Enable/disable buttons based on affordability
            if (kiranaButton != null)
                kiranaButton.interactable = playerMoney >= kiranaCost;
            
            if (pizzaButton != null)
                pizzaButton.interactable = playerMoney >= pizzaCost;
            
            if (cafeButton != null)
                cafeButton.interactable = playerMoney >= cafeCost;
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnKiranaButtonClicked()
        {
            Debug.Log("[BuildMenuManager] Kirana Store selected");
            StartBuildingPlacement("kirana", kiranaCost, kiranaPrefab);
        }
        
        private void OnPizzaButtonClicked()
        {
            Debug.Log("[BuildMenuManager] Pizza Outlet selected");
            StartBuildingPlacement("pizza", pizzaCost, pizzaPrefab);
        }
        
        private void OnCafeButtonClicked()
        {
            Debug.Log("[BuildMenuManager] Cafe selected");
            StartBuildingPlacement("cafe", cafeCost, cafePrefab);
        }
        
        private void StartBuildingPlacement(string buildingType, float cost, GameObject prefab)
        {
            if (placementManager == null)
            {
                Debug.LogError("[BuildMenuManager] BuildingPlacementManager is null!");
                return;
            }
            
            if (prefab == null)
            {
                Debug.LogError($"[BuildMenuManager] No prefab assigned for {buildingType}!");
                return;
            }
            
            // Check affordability
            if (!gameManager.CanAfford(cost))
            {
                Debug.LogWarning($"[BuildMenuManager] Cannot afford {buildingType} (₹{cost})");
                HUDManager.Instance?.ShowNotification($"Not enough money! Need ₹{cost:N0}", 2f);
                return;
            }
            
            // Start placement
            placementManager.StartPlacement(buildingType, cost, prefab);
            
            // Hide menu
            HideMenu();
        }
        
        #endregion
        
        #region Notifications
        
        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.OnPlayerDataUpdated += OnPlayerDataUpdated;
            }
        }
        
        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.OnPlayerDataUpdated -= OnPlayerDataUpdated;
            }
        }
        
        private void OnPlayerDataUpdated(AIBusinessTycoon.Data.PlayerTycoonData player)
        {
            if (isMenuOpen)
            {
                UpdateButtonStates();
            }
        }
        
        #endregion
        
        #region Helper UI
        
        private void OnGUI()
        {
            // Show hint when menu is closed
            if (!isMenuOpen && !Application.isEditor)
            {
                GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    normal = { textColor = new Color(1, 1, 1, 0.7f) }
                };
                
                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height - 30, 200, 30), 
                    "Press [B] or [Tab] to Build", hintStyle);
            }
        }
        
        #endregion
    }
}
