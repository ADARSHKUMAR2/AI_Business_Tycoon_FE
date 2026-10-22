using System.Collections;
using System.Collections.Generic;
using System;
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
        [SerializeField] private string currentPlayerId = "";
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
        public StoreInteractionManager CurrentStore => currentStore;
        private StoreInteractionManager currentStore;
        private readonly Dictionary<string, StoreInteractionManager> storesByBusinessId = new Dictionary<string, StoreInteractionManager>();
        private Coroutine worldDeliveryPollRoutine;
        private DateTime? nextDeliveryBoundaryUtc;
        private readonly Dictionary<string, DateTime> supplyWindowEndByBusinessId =
            new Dictionary<string, DateTime>();
        private bool deliveryBoundaryRequestInProgress;


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

        private void OnApplicationPause(bool paused)
        {
            if (!paused)
                RefreshAllStoresDeliveryStatus();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                RefreshAllStoresDeliveryStatus();
        }

        private void Start()
        {
            // Read player_id from PlayerPrefs set by TycoonAuthService on login
            string savedId = AIBusinessTycoon.Authentication.TycoonAuthService.GetSavedPlayerId();
            if (!string.IsNullOrEmpty(savedId))
                currentPlayerId = savedId;

            if (autoLoadOnStart)
                StartCoroutine(InitializeGame());
        }

        private void Update()
        {
            // Check delivery boundary
            if (nextDeliveryBoundaryUtc.HasValue &&
                !deliveryBoundaryRequestInProgress &&
                DateTime.UtcNow >= nextDeliveryBoundaryUtc.Value)
            {
                RefreshAllStoresDeliveryStatus();
            }

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

            // Step 3: Spawn existing businesses (but hold off on event businesses)
            if (CurrentPlayer.businesses != null)
            {
                foreach (BusinessData business in CurrentPlayer.businesses)
                {
                    // Do not spawn temporary event businesses immediately. 
                    // EventManager will spawn the CORRECT one once it polls the active event.
                    if (!business.is_event_business)
                    {
                        SpawnBuilding(business);
                    }
                }
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

            RefreshAllStoresDeliveryStatus();

            Debug.Log("[GameManager] === Game Initialized ===");
        }

        #endregion

        #region Store Enter / Exit

        /// <summary>
        /// Teleports avatar into the store and switches camera to MicroView.
        /// </summary>
        public void EnterStore(StoreInteractionManager store)
        {
            if (IsInStore || cameraController == null || !cameraController.IsMacroViewReady)
                return;

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

            CheckoutCounter checkoutCounter = store.GetComponentInChildren<CheckoutCounter>(true);
            checkoutCounter?.SetPlayerAtRegister(true);

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
                CheckoutCounter checkoutCounter = currentStore.GetComponentInChildren<CheckoutCounter>(true);
                checkoutCounter?.SetPlayerAtRegister(false);

                var storeCollider = currentStore.GetComponent<Collider>();
                if (storeCollider != null) storeCollider.enabled = true;

                var restockers = currentStore.GetComponentsInChildren<RestockerAI>(true);
                foreach (var restocker in restockers)
                {
                    // Restockers continue simulating in city view. Do not unbind them
                    // when the player leaves the store; only the player's camera/UI state
                    // changes here.
                    restocker.BindToStore(currentStore);
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

        private void RefreshAllStoresDeliveryStatus()
        {
            if (deliveryBoundaryRequestInProgress ||
                TycoonAPIService.Instance == null ||
                string.IsNullOrEmpty(CurrentPlayerId))
                return;

            deliveryBoundaryRequestInProgress = true;

            TycoonAPIService.Instance.GetAllDeliveryStatuses(
                CurrentPlayerId,
                response =>
                {
                    if (response?.businesses == null)
                    {
                        deliveryBoundaryRequestInProgress = false;
                        return;
                    }

                    foreach (DeliveryStatusResponse status in response.businesses)
                    {
                        if (status == null)
                            continue;

                        if (storesByBusinessId.TryGetValue(
                                status.business_id,
                                out StoreInteractionManager store))
                        {
                            store.ApplyDeliveryStatus(status);
                        }
                    }

                    nextDeliveryBoundaryUtc = FindEarliestDeliveryBoundary(response.businesses);
                    deliveryBoundaryRequestInProgress = false;
                },
                error =>
                {
                    deliveryBoundaryRequestInProgress = false;
                    Debug.LogWarning($"[GameManager] Batch delivery refresh failed: {error}");
                }
            );
        }

        private static DateTime? FindEarliestDeliveryBoundary(DeliveryStatusResponse[] statuses)
        {
            DateTime? earliest = null;
            DateTime now = DateTime.UtcNow;

            foreach (DeliveryStatusResponse status in statuses)
            {
                if (status == null)
                    continue;

                DateTime? nextDeliveryUtc = ParseUtc(status.next_delivery_at);
                if (nextDeliveryUtc.HasValue && nextDeliveryUtc.Value > now)
                    earliest = MinFutureBoundary(earliest, nextDeliveryUtc.Value);

                if (status.supply_available && status.supply_window_remaining > 0)
                {
                    DateTime supplyWindowEnd = now.AddSeconds(status.supply_window_remaining);
                    earliest = MinFutureBoundary(earliest, supplyWindowEnd);
                }
            }

            return earliest;
        }

        private static DateTime MinFutureBoundary(DateTime? current, DateTime candidate)
        {
            if (!current.HasValue || candidate < current.Value)
                return candidate;

            return current.Value;
        }

        private static DateTime? ParseUtc(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !DateTime.TryParse(
                    value,
                    null,
                    System.Globalization.DateTimeStyles.AssumeUniversal |
                    System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out DateTime parsed))
            {
                return null;
            }

            return parsed;
        }

        private void RefreshAllStoresAtDeliveryBoundary()
        {
            RefreshAllStoresDeliveryStatus();
        }
        public void RefreshPlayerData(Action<PlayerTycoonData> onSuccess = null, Action<string> onError = null)
        {
            if (IsLoading)
            {
                onError?.Invoke("Player data is still loading.");
                return;
            }

            apiService.GetPlayerData(
                currentPlayerId,
                data =>
                {
                    OnPlayerDataRefreshed(data);
                    onSuccess?.Invoke(data);
                },
                error =>
                {
                    OnPlayerDataRefreshError(error);
                    onError?.Invoke(error);
                });
        }

        public bool HasOwnedLandTile(int x, int y)
        {
            if (CurrentPlayer?.land_tiles == null)
                return false;

            foreach (LandTile tile in CurrentPlayer.land_tiles)
            {
                if (tile?.position != null && tile.position.x == x && tile.position.y == y)
                    return true;
            }

            return false;
        }

        public void AddOwnedLandTile(LandTile purchasedTile)
        {
            if (CurrentPlayer == null || purchasedTile?.position == null)
                return;

            if (CurrentPlayer.land_tiles == null)
                CurrentPlayer.land_tiles = new List<LandTile>();

            for (int i = 0; i < CurrentPlayer.land_tiles.Count; i++)
            {
                LandTile existingTile = CurrentPlayer.land_tiles[i];
                if (existingTile?.position != null
                    && existingTile.position.x == purchasedTile.position.x
                    && existingTile.position.y == purchasedTile.position.y)
                {
                    CurrentPlayer.land_tiles[i] = purchasedTile;
                    return;
                }
            }

            CurrentPlayer.land_tiles.Add(purchasedTile);
        }

        private void OnPlayerDataRefreshed(PlayerTycoonData data)
        {
            CurrentPlayer = data;
            ReconcileEventBusinesses(data);
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

            gridManager?.UpdateTileAfterBuildingPlaced(
                new Position { x = business.position_x, y = business.position_y },
                business.business_id
            );

            storesByBusinessId[business.business_id] = sim;

            spawnedBuildings[business.business_id] = buildingObj;
            sim.SpawnSavedEmployees(cashierPrefab, restockerPrefab, cleanerPrefab);
            RefreshStoreDeliveryStatus(sim);

            return buildingObj;
        }

        private void RefreshStoreDeliveryStatus(StoreInteractionManager store)
        {
            if (store == null || store.BusinessData == null ||
                TycoonAPIService.Instance == null || string.IsNullOrEmpty(CurrentPlayerId))
                return;

            string businessId = store.BusinessData.business_id;
            TycoonAPIService.Instance.GetDeliveryStatus(
                CurrentPlayerId,
                businessId,
                response =>
                {
                    if (response != null && storesByBusinessId.TryGetValue(businessId, out StoreInteractionManager registeredStore))
                        registeredStore.ApplyDeliveryStatus(response);
                },
                error => Debug.LogWarning($"[GameManager] Initial delivery refresh failed for {businessId}: {error}")
            );
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

        /// <summary>
        /// Returns the correct building prefab for an event franchise by matching
        /// the franchise name to a known business type keyword.
        /// </summary>
        public GameObject GetBuildingPrefabForEvent(string franchiseName)
        {
            string lower = franchiseName.ToLower();
            if (lower.Contains("pizza"))      return pizzaPrefab;
            if (lower.Contains("restaurant")) return restaurantPrefab;
            if (lower.Contains("kirana"))     return kiranaPrefab;
            return cafePrefab; // Default (Starbucks, Coffee, etc.)
        }

        /// <summary>
        /// Returns true if a building with the given business ID is already
        /// instantiated in the scene. Used to avoid duplicate spawns on re-login.
        /// </summary>
        public bool IsBusinessSpawned(string businessId)
        {
            return spawnedBuildings.ContainsKey(businessId);
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
        
        /// <summary>
        /// Spawns a temporary event business at a fixed world-space position,
        /// bypassing the grid tile system entirely. Focuses camera on it after spawning.
        /// </summary>
        public GameObject SpawnEventBusiness(BusinessData business, Vector3 worldPosition)
        {
            // Prevent duplicate spawns
            if (spawnedBuildings.ContainsKey(business.business_id))
            {
                Debug.Log($"[GameManager] Event business {business.business_id} already spawned.");
                return spawnedBuildings[business.business_id];
            }
            
            GameObject prefab = GetBuildingPrefab(business.business_type);
            if (prefab == null)
            {
                Debug.LogError($"[GameManager] No prefab found for business type: {business.business_type}");
                return null;
            }
            
            // Spawn at explicit world position - no grid conversion
            worldPosition.y = 0f;
            GameObject buildingObj = Instantiate(prefab, worldPosition, Quaternion.identity);
            buildingObj.name = $"{business.name} ({business.business_id}) [EVENT]";
            
            // Add marker component to identify it as an event business
            EventBusinessMarker marker = buildingObj.AddComponent<EventBusinessMarker>();
            marker.EventId = business.event_id;
            
            StoreInteractionManager sim = buildingObj.GetComponent<StoreInteractionManager>();
            if (sim == null) sim = buildingObj.AddComponent<StoreInteractionManager>();
            sim.BusinessData = business;
            
            storesByBusinessId[business.business_id] = sim;
            spawnedBuildings[business.business_id]   = buildingObj;
            
            sim.SpawnSavedEmployees(cashierPrefab, restockerPrefab, cleanerPrefab);
            RefreshStoreDeliveryStatus(sim);
            
            // Pan camera to the new event store so the player sees it
            CameraController.Instance?.FocusOnPosition(worldPosition);
            
            Debug.Log($"[GameManager] Event business spawned: {business.name} at {worldPosition}");
            return buildingObj;
        }
        
        /// <summary>
        /// Destroys all spawned event business prefabs for a given event_id.
        /// Called when an event ends.
        /// </summary>
        public void DespawnEventBusinesses(string eventId, bool inverse = false)
        {
            var toRemove = new System.Collections.Generic.List<string>();
            
            foreach (var kvp in storesByBusinessId)
            {
                BusinessData biz = kvp.Value?.BusinessData;
                if (biz != null && biz.is_event_business)
                {
                    if (eventId != null && !inverse && biz.event_id == eventId)
                        toRemove.Add(kvp.Key);
                    else if (inverse && biz.event_id != eventId)
                        toRemove.Add(kvp.Key);
                }
            }
            
            foreach (string bizId in toRemove)
            {
                if (spawnedBuildings.TryGetValue(bizId, out GameObject obj))
                {
                    Debug.Log($"[GameManager] Despawning event business: {bizId}");
                    Destroy(obj);
                    spawnedBuildings.Remove(bizId);
                }
                storesByBusinessId.Remove(bizId);
            }
        }

        public void ReconcileEventBusinesses(PlayerTycoonData refreshedPlayer)
        {
            if (refreshedPlayer?.businesses == null)
                return;

            var businessesById = new Dictionary<string, BusinessData>();
            foreach (BusinessData business in refreshedPlayer.businesses)
                businessesById[business.business_id] = business;

            var toRemove = new List<string>();
            foreach (KeyValuePair<string, StoreInteractionManager> entry in storesByBusinessId)
            {
                if (entry.Value == null ||
                    entry.Value.BusinessData == null ||
                    !entry.Value.BusinessData.is_event_business)
                    continue;

                if (!businessesById.TryGetValue(entry.Key, out BusinessData backendBusiness))
                {
                    toRemove.Add(entry.Key);
                    continue;
                }

                entry.Value.BusinessData = backendBusiness;
                if (!backendBusiness.is_event_business)
                {
                    entry.Value.gameObject.name = $"{backendBusiness.name} ({backendBusiness.business_id})";
                }
            }

            foreach (string businessId in toRemove)
            {
                if (spawnedBuildings.TryGetValue(businessId, out GameObject building))
                    Destroy(building);

                spawnedBuildings.Remove(businessId);
                storesByBusinessId.Remove(businessId);
            }
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
            // GUILayout.Label($"Player: {CurrentPlayer.name}", s);
            // GUILayout.Label($"Money: Rs.{CurrentPlayer.money:N0}", s);
            // GUILayout.Label($"Businesses: {CurrentPlayer.OwnedBusinessCount}", s);
            GUILayout.Label($"Mode: {(IsInStore ? "Inside Store (ESC to exit)" : "City View")}", s);
            // GUILayout.Label("Ctrl+R: Refresh | G: Toggle Grid", new GUIStyle(s) { fontSize = 8 });
            GUILayout.EndArea();
        }

        #endregion
    }
}
