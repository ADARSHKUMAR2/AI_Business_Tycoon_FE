using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AIBusinessTycoon.Services;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Config;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Master game controller that orchestrates all systems.
    /// Handles player state, game initialization, and system coordination.
    /// </summary>
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
        
        [Header("Building Prefabs")]
        [SerializeField] private GameObject kiranaPrefab;
        [SerializeField] private GameObject pizzaPrefab;
        [SerializeField] private GameObject cafePrefab;
        
        // State
        public PlayerTycoonData CurrentPlayer { get; private set; }
        public bool IsLoading { get; private set; }
        public bool IsGameReady { get; private set; }
        
        // Services
        private TycoonAPIService apiService;
        
        // Building instances
        private Dictionary<string, GameObject> spawnedBuildings = new Dictionary<string, GameObject>();
        
        // Events
        public System.Action<PlayerTycoonData> OnPlayerDataLoaded;
        public System.Action<PlayerTycoonData> OnPlayerDataUpdated;
        public System.Action<BusinessData> OnBusinessCreated;
        public System.Action<string> OnError;
        
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
            {
                StartCoroutine(InitializeGame());
            }
        }
        
        private void Update()
        {
            // Quick reload with R key
            if (Input.GetKeyDown(KeyCode.R) && Input.GetKey(KeyCode.LeftControl))
            {
                RefreshPlayerData();
            }
            
            // Toggle grid with G key
            if (Input.GetKeyDown(KeyCode.G))
            {
                if (gridManager != null)
                {
                    var visualizer = gridManager.GetComponent<GridVisualizer>();
                    if (visualizer != null)
                        visualizer.ToggleGrid();
                }
            }
            
            // DEV SHORTCUT: Buy Land at (1,0) when pressing 'L'
            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Log("[Dev Shortcut] Attempting to buy land at (1,0)...");
                LandPurchaseRequest req = new LandPurchaseRequest(CurrentPlayer.player_id, new Position { x = 1, y = 0 });
                
                apiService.PurchaseLand(req, 
                    (tile) => {
                        Debug.Log("Land purchased successfully!");
                        // Add to grid visually
                        if (gridManager != null) gridManager.AddLandTile(tile);
                        // Refresh stats
                        RefreshPlayerData();
                    },
                    (error) => Debug.LogError($"Failed to buy land: {error}")
                );
            }
            
            // DEV SHORTCUT: Buy Land at (0,1) when pressing 'K'
            if (Input.GetKeyDown(KeyCode.K))
            {
                Debug.Log("[Dev Shortcut] Attempting to buy land at (0,1)...");
                LandPurchaseRequest req = new LandPurchaseRequest(CurrentPlayer.player_id, new Position { x = 0, y = 1 });
                
                apiService.PurchaseLand(req, 
                    (tile) => {
                        Debug.Log("Land purchased successfully!");
                        if (gridManager != null) gridManager.AddLandTile(tile);
                        RefreshPlayerData();
                    },
                    (error) => Debug.LogError($"Failed to buy land: {error}")
                );
            }
        }

        
        #region Initialization
        
        private void InitializeServices()
        {
            apiService = TycoonAPIService.Instance;
            
            // Assign backend config via reflection
            if (backendConfig != null)
            {
                var field = apiService.GetType().GetField("backendConfig",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    field.SetValue(apiService, backendConfig);
                    Debug.Log($"[GameManager] BackendConfig assigned: {backendConfig.GetActiveURL()}");
                }
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
            Debug.Log("[GameManager] Step 1: Loading player data...");
            bool playerLoaded = false;
            string errorMessage = null;
            
            apiService.GetPlayerData(
                currentPlayerId,
                (data) => 
                {
                    CurrentPlayer = data;
                    playerLoaded = true;
                    Debug.Log($"[GameManager] Player loaded: {data.name} (₹{data.money:N0})");
                },
                (error) =>
                {
                    errorMessage = error;
                    playerLoaded = true;
                }
            );
            
            // Wait for player data
            yield return new WaitUntil(() => playerLoaded);
            
            if (CurrentPlayer == null)
            {
                Debug.LogError($"[GameManager] Failed to load player: {errorMessage}");
                OnError?.Invoke(errorMessage);
                IsLoading = false;
                yield break;
            }
            
            // Step 2: Initialize grid
            Debug.Log("[GameManager] Step 2: Initializing grid...");
            if (gridManager != null)
            {
                gridManager.InitializeFromPlayerData(CurrentPlayer);
            }
            else
            {
                Debug.LogWarning("[GameManager] GridManager not assigned!");
            }
            
            yield return new WaitForSeconds(0.2f);
            
            // Step 3: Spawn existing buildings
            Debug.Log("[GameManager] Step 3: Spawning existing buildings...");
            if (CurrentPlayer.businesses != null && CurrentPlayer.businesses.Count > 0)
            {
                Debug.Log($"[GameManager] Spawning {CurrentPlayer.businesses.Count} saved businesses.");
                foreach (BusinessData business in CurrentPlayer.businesses)
                {
                    SpawnBuilding(business);
                }
            }
            else
            {
                Debug.Log("[GameManager] No businesses to spawn.");
            }
            // For now, CurrentPlayer doesn't have businesses list in the simplified model
            // We'll add this when we integrate full player data with businesses
            
            // Step 4: Setup camera bounds
            Debug.Log("[GameManager] Step 4: Setting up camera...");
            if (cameraController != null && gridManager != null)
            {
                cameraController.SetBounds(
                    -gridManager.GridWidth / 2f, 
                    gridManager.GridWidth / 2f,
                    -gridManager.GridHeight / 2f, 
                    gridManager.GridHeight / 2f,
                    5f
                );
            }
            
            // Step 5: Notify listeners
            OnPlayerDataLoaded?.Invoke(CurrentPlayer);
            
            IsLoading = false;
            IsGameReady = true;
            
            Debug.Log("[GameManager] === Game Initialized Successfully ===");
        }
        
        #endregion
        
        #region Player Management
        
        /// <summary>
        /// Refresh player data from backend.
        /// </summary>
        public void RefreshPlayerData()
        {
            if (IsLoading)
            {
                Debug.LogWarning("[GameManager] Already loading data...");
                return;
            }
            
            Debug.Log("[GameManager] Refreshing player data...");
            
            apiService.GetPlayerData(
                currentPlayerId,
                OnPlayerDataRefreshed,
                OnPlayerDataRefreshError
            );
        }
        
        private void OnPlayerDataRefreshed(PlayerTycoonData data)
        {
            CurrentPlayer = data;
            Debug.Log($"[GameManager] Player data refreshed: ₹{data.money:N0}");
            OnPlayerDataUpdated?.Invoke(data);
        }
        
        private void OnPlayerDataRefreshError(string error)
        {
            Debug.LogError($"[GameManager] Failed to refresh player data: {error}");
            OnError?.Invoke(error);
        }
        
        /// <summary>
        /// Check if player can afford a purchase.
        /// </summary>
        public bool CanAfford(float cost)
        {
            if (CurrentPlayer == null) return false;
            return CurrentPlayer.money >= cost;
        }
        
        /// <summary>
        /// Deduct money locally (optimistic update, will sync with backend).
        /// </summary>
        public void DeductMoneyLocal(float amount)
        {
            if (CurrentPlayer != null)
            {
                CurrentPlayer.money -= amount;
                OnPlayerDataUpdated?.Invoke(CurrentPlayer);
            }
        }
        
        #endregion
        
        #region Building Management
        
        /// <summary>
        /// Spawn a building GameObject at the specified position.
        /// </summary>
        public GameObject SpawnBuilding(BusinessData business)
        {
            if (spawnedBuildings.ContainsKey(business.business_id))
            {
                Debug.LogWarning($"[GameManager] Building {business.business_id} already spawned");
                return spawnedBuildings[business.business_id];
            }
            
            GameObject prefab = GetBuildingPrefab(business.business_type);
            
            if (prefab == null)
            {
                Debug.LogError($"[GameManager] No prefab found for business type: {business.business_type}");
                return null;
            }
            
            Vector3 worldPos = gridManager.GridToWorldPosition(business.position_x, business.position_y);
            worldPos.y = 0.5f; // Elevate slightly above ground
            
            GameObject buildingObj = Instantiate(prefab, worldPos, Quaternion.identity);
            buildingObj.name = $"{business.name} ({business.business_id})";
            
            // TODO: Add BuildingView component to handle business logic
            
            spawnedBuildings[business.business_id] = buildingObj;
            
            Debug.Log($"[GameManager] Spawned building: {business.name} at ({business.position_x}, {business.position_y})");
            
            return buildingObj;
        }
        
        /// <summary>
        /// Get building prefab based on business type.
        /// </summary>
        private GameObject GetBuildingPrefab(string businessType)
        {
            switch (businessType.ToLower())
            {
                case "kirana":
                    return kiranaPrefab;
                case "pizza":
                    return pizzaPrefab;
                case "cafe":
                    return cafePrefab;
                default:
                    Debug.LogWarning($"[GameManager] Unknown business type: {businessType}, using kirana prefab");
                    return kiranaPrefab;
            }
        }
        
        /// <summary>
        /// Handle business creation from API.
        /// </summary>
        public void OnBusinessCreatedCallback(BusinessData business)
        {
            Debug.Log($"[GameManager] Business created: {business.name}");
            
            // Update grid
            if (gridManager != null)
            {
                gridManager.UpdateTileAfterBuildingPlaced(
                    new Position { x = business.position_x, y = business.position_y },
                    business.business_id
                );
            }
            
            // Spawn building
            SpawnBuilding(business);
            
            // Refresh player data to get updated stats
            RefreshPlayerData();
            
            // Notify listeners
            OnBusinessCreated?.Invoke(business);
        }
        
        #endregion
        
        #region Public Getters
        
        public GridManager GetGridManager() => gridManager;
        public CameraController GetCameraController() => cameraController;
        public BuildingPlacementManager GetBuildingPlacementManager() => buildingPlacementManager;
        public TycoonAPIService GetAPIService() => apiService;
        
        #endregion
        
        #region Debug
        
        private void OnGUI()
        {
            if (!IsGameReady) return;
            
            // Debug info in bottom-left corner
            GUIStyle debugStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = Color.white }
            };
            
            GUILayout.BeginArea(new Rect(10, Screen.height - 100, 300, 100));
            
            if (CurrentPlayer != null)
            {
                GUILayout.Label($"Player: {CurrentPlayer.name}", debugStyle);
                GUILayout.Label($"Money: ₹{CurrentPlayer.money:N0}", debugStyle);
                GUILayout.Label($"Businesses: {CurrentPlayer.OwnedBusinessCount}", debugStyle);
            }
            
            GUILayout.Label("Ctrl+R: Refresh | G: Toggle Grid", new GUIStyle(debugStyle) { fontSize = 8 });
            
            GUILayout.EndArea();
        }
        
        #endregion
    }
}
