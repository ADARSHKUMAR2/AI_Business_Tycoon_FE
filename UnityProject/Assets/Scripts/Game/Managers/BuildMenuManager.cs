using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace AIBusinessTycoon.UI
{
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
        [SerializeField] private Button restaurantButton;

        [Header("Building Info")]
        [SerializeField] private TextMeshProUGUI kiranaInfoText;
        [SerializeField] private TextMeshProUGUI pizzaInfoText;
        [SerializeField] private TextMeshProUGUI cafeInfoText;
        [SerializeField] private TextMeshProUGUI restaurantInfoText;

        [Header("Building Prefabs")]
        [SerializeField] private GameObject kiranaPrefab;
        [SerializeField] private GameObject pizzaPrefab;
        [SerializeField] private GameObject cafePrefab;
        [SerializeField] private GameObject restaurantPrefab;

        [Header("Building Costs")]
        [SerializeField] private float kiranaCost = 5000f;
        [SerializeField] private float pizzaCost = 15000f;
        [SerializeField] private float cafeCost = 25000f;
        [SerializeField] private float restaurantCost = 18000f;

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
            else if (Instance != this)
            {
                Debug.LogWarning("[BuildMenuManager] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            gameManager = Managers.GameManager.Instance;
            placementManager = Managers.BuildingPlacementManager.Instance;

            SafeInit();
            HideMenu();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey) || Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleMenu();
            }

            if (placementManager != null && placementManager.IsPlacing && isMenuOpen)
            {
                HideMenu();
            }
        }

        /// <summary>
        /// This ensures references exist even if Unity drops them at runtime.
        /// Call this before opening the menu.
        /// </summary>
        private void SafeInit()
        {
            // If menuPanel is null, try to find it dynamically by name
            if (menuPanel == null)
            {
                var panelTransform = transform.Find("BuildMenuPanel");
                if (panelTransform != null) menuPanel = panelTransform.gameObject;
            }

            // Only attempt to bind button listeners if they exist and haven't been bound yet
            if (kiranaButton != null)
            {
                kiranaButton.onClick.RemoveAllListeners();
                kiranaButton.onClick.AddListener(OnKiranaButtonClicked);
            }

            if (pizzaButton != null)
            {
                pizzaButton.onClick.RemoveAllListeners();
                pizzaButton.onClick.AddListener(OnPizzaButtonClicked);
            }

            if (cafeButton != null)
            {
                cafeButton.onClick.RemoveAllListeners();
                cafeButton.onClick.AddListener(OnCafeButtonClicked);
            }

            if (restaurantButton != null)
            {
                restaurantButton.onClick.RemoveAllListeners();
                restaurantButton.onClick.AddListener(OnRestaurantButtonClicked);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(HideMenu);
            }

            UpdateBuildingInfo();
        }

        public void ShowMenu()
        {
            SafeInit(); // Re-check before showing

            if (menuPanel != null)
            {
                menuPanel.SetActive(true);
                isMenuOpen = true;
                UpdateButtonStates();
                Debug.Log("[BuildMenuManager] Menu opened");
            }
            else
            {
                Debug.LogError("[BuildMenuManager] Cannot show menu: menuPanel is NULL!");
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

        private void UpdateBuildingInfo()
        {
            if (kiranaInfoText != null)
                kiranaInfoText.text = $"Kirana Store\n₹{kiranaCost:N0}\n\nBasic grocery shop\nfor daily needs";

            if (pizzaInfoText != null)
                pizzaInfoText.text = $"Pizza Outlet\n₹{pizzaCost:N0}\n\nFast food restaurant\nwith delivery";

            if (cafeInfoText != null)
                cafeInfoText.text = $"Cafe\n₹{cafeCost:N0}\n\nCozy coffee shop\nwith seating";

            if (restaurantInfoText != null)
                restaurantInfoText.text = $"Restaurant\n₹{restaurantCost:N0}\n\nIngredient-based kitchen\nwith harvest & stock";
        }

        private void UpdateButtonStates()
        {
            if (gameManager == null || gameManager.CurrentPlayer == null) return;

            float playerMoney = gameManager.CurrentPlayer.money;

            if (kiranaButton != null) kiranaButton.interactable = playerMoney >= kiranaCost;
            if (pizzaButton != null) pizzaButton.interactable = playerMoney >= pizzaCost;
            if (cafeButton != null) cafeButton.interactable = playerMoney >= cafeCost;
            if (restaurantButton != null) restaurantButton.interactable = playerMoney >= restaurantCost;
        }

        private void OnKiranaButtonClicked() => StartBuildingPlacement("kirana", kiranaCost, kiranaPrefab);
        private void OnPizzaButtonClicked() => StartBuildingPlacement("pizza", pizzaCost, pizzaPrefab);
        private void OnCafeButtonClicked() => StartBuildingPlacement("cafe", cafeCost, cafePrefab);
        private void OnRestaurantButtonClicked() => StartBuildingPlacement("restaurant", restaurantCost, restaurantPrefab);

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

            if (gameManager == null)
            {
                Debug.LogError("[BuildMenuManager] GameManager is null!");
                return;
            }

            if (!gameManager.CanAfford(cost))
            {
                Debug.LogWarning($"[BuildMenuManager] Cannot afford {buildingType} (₹{cost})");
                HUDManager.Instance?.ShowNotification($"Not enough money! Need ₹{cost:N0}", 2f);
                return;
            }

            placementManager.StartPlacement(buildingType, cost, prefab);
            HideMenu();
        }

        private void OnEnable()
        {
            if (gameManager != null)
                gameManager.OnPlayerDataUpdated += OnPlayerDataUpdated;
        }

        private void OnDisable()
        {
            if (gameManager != null)
                gameManager.OnPlayerDataUpdated -= OnPlayerDataUpdated;
        }

        private void OnPlayerDataUpdated(AIBusinessTycoon.Data.PlayerTycoonData player)
        {
            if (isMenuOpen)
                UpdateButtonStates();
        }

        private void OnGUI()
        {
            if (!isMenuOpen && !Application.isEditor)
            {
                GUIStyle hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    normal = { textColor = new Color(1, 1, 1, 0.7f) }
                };

                GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height - 30, 220, 30),
                    "Press [B] or [Tab] to Build", hintStyle);
            }
        }
    }
}
