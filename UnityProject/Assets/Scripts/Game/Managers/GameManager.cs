using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AIBusinessTycoon.Services;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Config;

namespace AIBusinessTycoon.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private BackendConfig backendConfig;

        [Header("Player Settings")]
        [SerializeField] private string currentPlayerId = "player_0eb8eddc";
        [SerializeField] private bool autoLoadOnStart = true;

        [Header("Manager References")]
        [SerializeField] private GridManager gridManager;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private BuildingPlacementManager buildingPlacementManager;
        [SerializeField] private PlayerController playerController;

        [SerializeField] private DeliveryHUDController deliveryHud;

        [Header("Building Prefabs")]
        [SerializeField] private GameObject kiranaPrefab;
        [SerializeField] private GameObject pizzaPrefab;
        [SerializeField] private GameObject cafePrefab;
        [SerializeField] private GameObject restaurantPrefab;

        [Header("Employee Prefabs")]
        [SerializeField] public GameObject cashierPrefab;
        [SerializeField] public GameObject restockerPrefab;
        [SerializeField] public GameObject cleanerPrefab;

        // State
        public PlayerTycoonData CurrentPlayer { get; private set; }
        public bool IsLoading { get; private set; }
        public bool IsGameReady { get; private set; }
        public bool IsInStore { get; private set; }
        private StoreInteractionManager currentStore;
        private readonly Dictionary<string, StoreInteractionManager> storesByBusinessId = new Dictionary<string, StoreInteractionManager>();
        private readonly Dictionary<string, float> nextPollAt = new Dictionary<string, float>();

        private Coroutine worldDeliveryPollRoutine;


        // Services
        private TycoonAPIService apiService;

        // Building instances
        private Dictionary<string, GameObject> spawnedBuildings = new Dictionary<string, GameObject>();

        // Events
        public System.Action<PlayerTycoonData> OnPlayerDataLoaded;
        public System.Action<PlayerTycoonData> OnPlayerDataUpdated;
        public System.Action<BusinessData>     OnBusinessCreated;
        public System.Action<string>           OnError;
        public System.Action                   OnEnteredStore;
        public System.Action                   OnExitedStore;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeServices();
        }

        private void Start()
        {
            if (autoLoadOnStart)
                StartCoroutine(InitializeGame());
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && IsInStore)
                ExitStore();

            if (Input.GetKeyDown(KeyCode.R) && Input.GetKey(KeyCode.LeftControl))
                RefreshPlayerData();

            if (Input.GetKeyDown(KeyCode.G))
            {
                if (gridManager != null)
                {
                    var viz = gridManager.GetComponent<GridVisualizer>();
                    if (viz != null) viz.ToggleGrid();
                }
            }

            // Dev shortcuts
            if (Input.GetKeyDown(KeyCode.L) && CurrentPlayer != null)
            {
                LandPurchaseRequest req = new LandPurchaseRequest(CurrentPlayer.player_id, new Position { x = 1, y = 0 });
                apiService.PurchaseLand(req,
                    (tile) => { gridManager?.OnLandPurchaseSuccess(tile); RefreshPlayerData(); },
                    (err)  => Debug.LogError($"Land purchase failed: {err}")
                );
            }

            if (Input.GetKeyDown(KeyCode.K) && CurrentPlayer != null)
            {
                LandPurchaseRequest req = new LandPurchaseRequest(CurrentPlayer.player_id, new Position { x = 0, y = 1 });
                apiService.PurchaseLand(req,
                    (tile) => { gridManager?.OnLandPurchaseSuccess(tile); RefreshPlayerData(); },
                    (err)  => Debug.LogError($"Land purchase failed: {err}")
                );
            }
        }

        public string CurrentPlayerId
        {
            get
            {
                if (CurrentPlayer == null) return string.Empty;
                return CurrentPlayer.player_id;
            }
        }

        public string ActiveBusinessId
        {
            get
            {
                if (currentStore == null) return string.Empty;
                if (currentStore.BusinessData == null) return string.Empty;
                return currentStore.BusinessData.business_id;
            }
        }

        #region Initialization

        private void InitializeServices()
        {
            apiService = TycoonAPIService.Instance;

            if (backendConfig != null)
            {
                var field = apiService.GetType().GetField("backendConfig",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(apiService, backendConfig);
                Debug.Log($"[GameManager] BackendConfig assigned: {backendConfig.GetActiveURL()}");
            }
            else
            {
                Debug.LogError("[GameManager] BackendConfig is not assigned!");
            }
        }

        private IEnumerator InitializeGame()
        {
            Debug.Log("[GameManager] === Initializing Game ===");
            IsLoading = true;
            IsGameReady = false;

            // Step 1: Load player data
            bool playerLoaded = false;
            string errorMessage = null;

            apiService.GetPlayerData(currentPlayerId,
                (data) => { CurrentPlayer = data; playerLoaded = true; },
                (err)  => { errorMessage = err; playerLoaded = true; }
            );

            yield return new WaitUntil(() => playerLoaded);

            if (CurrentPlayer == null)
            {
                Debug.LogError($"[GameManager] Failed to load player: {errorMessage}");
                OnError?.Invoke(errorMessage);
                IsLoading = false;
                yield break;
            }

            // Step 2: Initialize grid
            if (gridManager != null)
                gridManager.InitializeFromPlayerData(CurrentPlayer);

            yield return new WaitForSeconds(0.1f);

            // Step 3: Spawn existing buildings
            if (CurrentPlayer.businesses != null)
            {
                foreach (BusinessData business in CurrentPlayer.businesses)
                    SpawnBuilding(business);
            }

            // Step 4: Camera bounds
            if (cameraController != null && gridManager != null)
            {
                gridManager.UpdateCameraBoundsToOwnedLand(10f);
            }

            // Step 5: Deactivate player avatar (only active in stores)
            if (playerController != null)
                playerController.Deactivate();

            OnPlayerDataLoaded?.Invoke(CurrentPlayer);
            IsLoading  = false;
            IsGameReady = true;

            if (worldDeliveryPollRoutine != null)
                StopCoroutine(worldDeliveryPollRoutine);

            worldDeliveryPollRoutine = StartCoroutine(PollAllStoresDeliveryStatus());

            Debug.Log("[GameManager] === Game Initialized ===");
        }

        #endregion

        #region Store Enter / Exit

        /// <summary>
        /// Teleports avatar into the store and switches camera to MicroView.
        /// </summary>
        public void EnterStore(StoreInteractionManager store)
        {
            if (IsInStore) return;

            if (store == null)
            {
                Debug.LogWarning("[GameManager] Tried to enter null store.");
                return;
            }

            IsInStore = true;
            currentStore = store;

            if (deliveryHud != null)
            {
                deliveryHud.Unbind();
                deliveryHud.BindToStore(store);
            }
             
            // Bind restocker AIs to this store
            var restockers = store.GetComponentsInChildren<RestockerAI>(true);
            foreach (var restocker in restockers)
            {
                restocker.BindToStore(store);
            }

            var storeCollider = currentStore.GetComponent<Collider>();
            if (storeCollider != null) storeCollider.enabled = false;

            UI.BuildMenuManager.Instance?.HideMenu();
            UI.HUDManager.Instance?.ShowHUD(false);

            if (playerController != null)
                playerController.ActivateAtPosition(store.GetEntrancePosition());

            if (cameraController != null && playerController != null)
                cameraController.EnterMicroView(playerController.transform);

            UI.StoreUIManager.Instance?.OpenStoreUI(store);

            OnEnteredStore?.Invoke();
            Debug.Log($"[GameManager] Entered store: {store.BusinessData?.name}");
        }

        public void ExitStore()
        {
            if (!IsInStore) return;

            if (deliveryHud != null)
            {
                deliveryHud.Unbind();
            }

            if (currentStore != null)
            {
                var storeCollider = currentStore.GetComponent<Collider>();
                if (storeCollider != null) storeCollider.enabled = true;

                var restockers = currentStore.GetComponentsInChildren<RestockerAI>(true);
                foreach (var restocker in restockers)
                {
                    restocker.BindToStore(null);
                }
            }

            IsInStore = false;
            currentStore = null;

            UI.StoreUIManager.Instance?.CloseStoreUI();
            UI.HUDManager.Instance?.ShowHUD(true);

            if (playerController != null)
                playerController.Deactivate();

            if (cameraController != null)
                cameraController.ExitToMacroView();

            OnExitedStore?.Invoke();
            Debug.Log("[GameManager] Exited store.");
        }

        #endregion

        #region Player Management

        private IEnumerator PollAllStoresDeliveryStatus()
        {
            while (true)
            {
                float now = Time.time;

                foreach (var pair in storesByBusinessId)
                {
                    var store = pair.Value;
                    if (store == null || store.BusinessData == null)
                        continue;

                    if (!nextPollAt.TryGetValue(store.BusinessData.business_id, out float nextAt) || now >= nextAt)
                    {
                        nextPollAt[store.BusinessData.business_id] = now + 15f; // or 10f / 20f
                        PollSingleStore(store);
                    }
                }

                yield return new WaitForSeconds(1f); // cheap scheduler tick
            }
        }

        private void PollSingleStore(StoreInteractionManager store)
        {
            if (TycoonAPIService.Instance == null || string.IsNullOrEmpty(CurrentPlayerId))
                return;

            TycoonAPIService.Instance.GetDeliveryStatus(
                CurrentPlayerId,
                store.BusinessData.business_id,
                response =>
                {
                    if (response != null)
                        store.ApplyDeliveryStatus(response);
                },
                error =>
                {
                    Debug.LogWarning($"Delivery poll failed for {store.BusinessData.business_id}: {error}");
                }
            );
        }
        public void RefreshPlayerData()
        {
            if (IsLoading) return;
            apiService.GetPlayerData(currentPlayerId, OnPlayerDataRefreshed, OnPlayerDataRefreshError);
        }

        private void OnPlayerDataRefreshed(PlayerTycoonData data)
        {
            CurrentPlayer = data;
            OnPlayerDataUpdated?.Invoke(data);
        }

        private void OnPlayerDataRefreshError(string error)
        {
            Debug.LogError($"[GameManager] Refresh failed: {error}");
            OnError?.Invoke(error);
        }

        public bool CanAfford(float cost)
            => CurrentPlayer != null && CurrentPlayer.money >= cost;

        public void DeductMoneyLocal(float amount)
        {
            if (CurrentPlayer == null) return;
            CurrentPlayer.money -= amount;

            // If amount is negative, it means we EARNED money (revenue)
            if (amount < 0)
            {
                // Ensure stats exists
                if (CurrentPlayer.stats == null) 
                    CurrentPlayer.stats = new PlayerStats();
                    
                CurrentPlayer.stats.total_revenue += Mathf.Abs(amount);
            }

            OnPlayerDataUpdated?.Invoke(CurrentPlayer);

            if (apiService != null)
            {
                apiService.UpdatePlayerData(CurrentPlayer, (success) => {
                    if (!success)
                    {
                        Debug.LogWarning("[GameManager] Failed to sync money/revenue to backend.");
                    }
                });
            }
        }

        #endregion

        #region Building Management

        public GameObject SpawnBuilding(BusinessData business)
        {
            if (spawnedBuildings.ContainsKey(business.business_id))
                return spawnedBuildings[business.business_id];

            GameObject prefab = GetBuildingPrefab(business.business_type);
            if (prefab == null) return null;

            Vector3 worldPos = gridManager.GridToWorldPosition(business.position_x, business.position_y);
            worldPos.y = 0f;

            GameObject buildingObj = Instantiate(prefab, worldPos, Quaternion.identity);
            buildingObj.name = $"{business.name} ({business.business_id})";

            StoreInteractionManager sim = buildingObj.GetComponent<StoreInteractionManager>();
            if (sim == null) sim = buildingObj.AddComponent<StoreInteractionManager>();
            sim.BusinessData = business;

            storesByBusinessId[business.business_id] = sim;

            spawnedBuildings[business.business_id] = buildingObj;
            sim.SpawnSavedEmployees(cashierPrefab, restockerPrefab, cleanerPrefab);

            return buildingObj;
        }

        private GameObject GetBuildingPrefab(string businessType)
        {
            return businessType.ToLower() switch
            {
                "kirana" => kiranaPrefab,
                "pizza"  => pizzaPrefab,
                "cafe"   => cafePrefab,
                "restaurant" => restaurantPrefab,
                _        => kiranaPrefab
            };
        }

        public void OnBusinessCreatedCallback(BusinessData business)
        {
            gridManager?.UpdateTileAfterBuildingPlaced(
                new Position { x = business.position_x, y = business.position_y },
                business.business_id
            );

            SpawnBuilding(business);
            RefreshPlayerData();
            OnBusinessCreated?.Invoke(business);
        }

        #endregion

        #region Public Getters

        public GridManager              GetGridManager()             => gridManager;
        public CameraController         GetCameraController()        => cameraController;
        public BuildingPlacementManager GetBuildingPlacementManager() => buildingPlacementManager;
        public TycoonAPIService         GetAPIService()              => apiService;

        #endregion

        #region Debug GUI

        private void OnGUI()
        {
            if (!IsGameReady || CurrentPlayer == null) return;

            GUIStyle s = new GUIStyle(GUI.skin.label) { fontSize = 10, normal = { textColor = Color.white } };
            GUILayout.BeginArea(new Rect(10, Screen.height - 120, 320, 120));
            GUILayout.Label($"Player: {CurrentPlayer.name}", s);
            GUILayout.Label($"Money: Rs.{CurrentPlayer.money:N0}", s);
            GUILayout.Label($"Businesses: {CurrentPlayer.OwnedBusinessCount}", s);
            GUILayout.Label($"Mode: {(IsInStore ? "Inside Store (ESC to exit)" : "City View")}", s);
            GUILayout.Label("Ctrl+R: Refresh | G: Toggle Grid", new GUIStyle(s) { fontSize = 8 });
            GUILayout.EndArea();
        }

        #endregion
    }
}
