using UnityEngine;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Manages building placement with ghost preview and validation.
    /// Handles the flow: Select building → Preview → Validate → Place → API call.
    /// </summary>
    public class BuildingPlacementManager : MonoBehaviour
    {
        public static BuildingPlacementManager Instance { get; private set; }
        
        [Header("Placement Settings")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float ghostAlpha = 0.5f;
        
        [Header("Visual Feedback")]
        [SerializeField] private Color validPlacementColor = new Color(0, 1, 0, 0.5f);
        [SerializeField] private Color invalidPlacementColor = new Color(1, 0, 0, 0.5f);
        
        // State
        private bool isPlacingBuilding = false;
        private string currentBuildingType;
        private float currentBuildingCost;
        private GameObject ghostBuilding;
        private Position currentGridPosition;
        private bool isValidPlacement;
        
        // References
        private GridManager gridManager;
        private GameManager gameManager;
        private Camera mainCamera;
        
        // Events
        public System.Action OnPlacementStarted;
        public System.Action OnPlacementCancelled;
        public System.Action<BusinessData> OnPlacementCompleted;
        
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
            gridManager = GridManager.Instance;
            gameManager = GameManager.Instance;
            mainCamera = Camera.main;
            
            if (gridManager == null)
            {
                Debug.LogError("[BuildingPlacementManager] GridManager not found!");
            }
            
            if (gameManager == null)
            {
                Debug.LogError("[BuildingPlacementManager] GameManager not found!");
            }
        }
        
        private void Update()
        {
            if (!isPlacingBuilding) return;
            
            UpdateGhostPosition();
            HandlePlacementInput();
        }
        
        #region Placement Flow
        
        /// <summary>
        /// Start building placement mode.
        /// </summary>
        public void StartPlacement(string buildingType, float cost, GameObject buildingPrefab)
        {
            if (isPlacingBuilding)
            {
                Debug.LogWarning("[BuildingPlacementManager] Already placing a building!");
                return;
            }
            
            // Check if player can afford
            if (!gameManager.CanAfford(cost))
            {
                Debug.LogWarning($"[BuildingPlacementManager] Cannot afford {buildingType} (₹{cost})");
                return;
            }
            
            isPlacingBuilding = true;
            currentBuildingType = buildingType;
            currentBuildingCost = cost;
            
            // Create ghost building
            CreateGhostBuilding(buildingPrefab);
            
            Debug.Log($"[BuildingPlacementManager] Started placement: {buildingType}");
            OnPlacementStarted?.Invoke();
        }
        
        /// <summary>
        /// Cancel current placement.
        /// </summary>
        public void CancelPlacement()
        {
            if (!isPlacingBuilding) return;
            
            DestroyGhostBuilding();
            
            if (gridManager != null)
            {
                gridManager.ClearHighlight();
            }
            
            isPlacingBuilding = false;
            currentBuildingType = null;
            currentGridPosition = null;
            
            Debug.Log("[BuildingPlacementManager] Placement cancelled");
            OnPlacementCancelled?.Invoke();
        }
        
        #endregion
        
        #region Ghost Building
        
        private void CreateGhostBuilding(GameObject prefab)
        {
            if (prefab == null)
            {
                Debug.LogError("[BuildingPlacementManager] Building prefab is null!");
                return;
            }
            
            ghostBuilding = Instantiate(prefab);
            ghostBuilding.name = "Ghost_" + prefab.name;
            
            // Make it semi-transparent
            SetGhostMaterialAlpha(ghostBuilding, ghostAlpha);
            
            // Disable colliders
            foreach (var collider in ghostBuilding.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
        }
        
        private void DestroyGhostBuilding()
        {
            if (ghostBuilding != null)
            {
                Destroy(ghostBuilding);
                ghostBuilding = null;
            }
        }
        
        private void SetGhostMaterialAlpha(GameObject obj, float alpha)
        {
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in renderer.materials)
                {
                    Color color = mat.color;
                    color.a = alpha;
                    mat.color = color;
                    
                    // Enable transparency
                    mat.SetFloat("_Mode", 3); // Transparent mode
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                }
            }
        }
        
        private void UpdateGhostMaterialColor(Color color)
        {
            if (ghostBuilding == null) return;
            
            foreach (var renderer in ghostBuilding.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in renderer.materials)
                {
                    mat.color = color;
                }
            }
        }
        
        #endregion
        
        #region Position Update
        
        private void UpdateGhostPosition()
        {
            if (ghostBuilding == null || gridManager == null) return;
            
            // Get grid position under mouse
            Position gridPos = gridManager.GetGridPositionFromMouse();
            
            if (gridPos == null)
            {
                ghostBuilding.SetActive(false);
                return;
            }
            
            ghostBuilding.SetActive(true);
            currentGridPosition = gridPos;
            
            // Move ghost to grid position
            Vector3 worldPos = gridManager.GridToWorldPosition(gridPos);
            worldPos.y = 0.5f; // Elevate slightly
            ghostBuilding.transform.position = worldPos;
            
            // Validate placement
            isValidPlacement = gridManager.IsValidBuildingPlacement(gridPos);
            
            // Update visual feedback
            Color feedbackColor = isValidPlacement ? validPlacementColor : invalidPlacementColor;
            UpdateGhostMaterialColor(feedbackColor);
            gridManager.HighlightTile(gridPos, isValidPlacement);
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandlePlacementInput()
        {
            // Left click to place
            if (Input.GetMouseButtonDown(0))
            {
                if (isValidPlacement && currentGridPosition != null)
                {
                    PlaceBuilding();
                }
                else
                {
                    Debug.LogWarning("[BuildingPlacementManager] Invalid placement position!");
                }
            }
            
            // Right click or ESC to cancel
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacement();
            }
        }
        
        #endregion
        
        #region API Integration
        
        private void PlaceBuilding()
        {
            if (currentGridPosition == null || gameManager == null) return;
            
            Debug.Log($"[BuildingPlacementManager] Placing {currentBuildingType} at ({currentGridPosition.x}, {currentGridPosition.y})");
            
            // Create request
            BusinessCreateRequest request = new BusinessCreateRequest(
                gameManager.CurrentPlayer.player_id,
                currentBuildingType,
                $"My {currentBuildingType} Store",
                currentGridPosition.x,
                currentGridPosition.y
            );
            
            // Optimistic update: deduct money locally
            gameManager.DeductMoneyLocal(currentBuildingCost);
            
            // Clean up placement mode
            Position placedPosition = currentGridPosition;
            CancelPlacement();
            
            // Call API
            gameManager.GetAPIService().CreateBusiness(
                request,
                (business) => OnBuildingPlaced(business),
                (error) => OnBuildingPlacementError(error, placedPosition)
            );
        }
        
        private void OnBuildingPlaced(BusinessData business)
        {
            Debug.Log($"[BuildingPlacementManager] Building placed successfully: {business.business_id}");
            
            // Notify GameManager to spawn building and refresh data
            gameManager.OnBusinessCreatedCallback(business);
            
            OnPlacementCompleted?.Invoke(business);
        }
        
        private void OnBuildingPlacementError(string error, Position attemptedPosition)
        {
            Debug.LogError($"[BuildingPlacementManager] Failed to place building: {error}");
            
            // Refund money (optimistic update failed)
            gameManager.RefreshPlayerData();
            
            // Show error to player
            // TODO: Integrate with UI notification system
        }
        
        #endregion
        
        #region Public Getters
        
        public bool IsPlacing => isPlacingBuilding;
        public string CurrentBuildingType => currentBuildingType;
        
        #endregion
    }
}
