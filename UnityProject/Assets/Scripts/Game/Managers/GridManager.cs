using System.Collections.Generic;
using UnityEngine;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.Managers
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }
        
        [Header("Grid Settings")]
        [SerializeField] private int gridWidth = 20;
        [SerializeField] private int gridHeight = 20;
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private Vector3 gridOrigin = Vector3.zero;
        
        [Header("Visual Settings")]
        [SerializeField] private Material emptyTileMaterial;
        [SerializeField] private Material ownedTileMaterial;
        [SerializeField] private Material validPlacementMaterial;
        [SerializeField] private Material invalidPlacementMaterial;
        [SerializeField] private Material purchasableTileMaterial; // NEW: Blue glow for buyable tiles
        
        [Header("Prefabs")]
        [SerializeField] private GameObject tilePrefab;
        
        // Data structures
        private Dictionary<string, LandTile> landTiles = new Dictionary<string, LandTile>();
        private Dictionary<string, GameObject> tileVisuals = new Dictionary<string, GameObject>();
        
        // Highlight tracking
        private GameObject currentHighlightedTile;
        private string currentHighlightedKey;
        
        // Purchasable tile visuals (temporary quads for unowned adjacent tiles)
        private Dictionary<string, GameObject> purchasableVisuals = new Dictionary<string, GameObject>();
        
        // Layers
        private LayerMask gridLayer;
        
        // State
        private bool isInLandPurchaseMode = false;
        
        // Events
        public System.Action<Position, float> OnTileClickedForPurchase; // Position + cost

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        private void Start()
        {
            gridLayer = LayerMask.GetMask("Grid");
        }

        private void Update()
        {
            // Only handle land purchase clicks when NOT placing a building
            if (BuildingPlacementManager.Instance != null && BuildingPlacementManager.Instance.IsPlacing)
                return;

            HandleLandPurchaseHover();
            HandleLandPurchaseClick();
        }
        
        #region Initialization
        
        public void InitializeFromPlayerData(PlayerTycoonData playerData)
        {
            Debug.Log($"[GridManager] Initializing grid for player: {playerData.player_id}");
            
            ClearGrid();
            
            if (playerData.land_tiles == null || playerData.land_tiles.Count == 0)
            {
                Debug.Log("[GridManager] Player has no land. Creating starter tile at (0,0)");
                CreateStarterTile();
            }
            else
            {
                Debug.Log($"[GridManager] Found {playerData.land_tiles.Count} land tiles to draw.");
                foreach (LandTile tile in playerData.land_tiles)
                {
                    AddLandTile(tile);
                }
            }
            
            // After drawing owned tiles, show adjacent purchasable tiles
            RefreshPurchasableTiles();
            
            Debug.Log($"[GridManager] Grid initialized with {landTiles.Count} tiles");
        }
        
        private void CreateStarterTile()
        {
            LandTile starterTile = new LandTile
            {
                position = new Position { x = 0, y = 0 },
                tile_type = "empty",
                purchase_cost = 0f,
                purchased_at = System.DateTime.UtcNow.ToString("o"),
                business_id = null
            };
            AddLandTile(starterTile);
        }
        
        public void AddLandTile(LandTile tile)
        {
            string key = GetPositionKey(tile.position);
            
            if (landTiles.ContainsKey(key))
            {
                Debug.LogWarning($"[GridManager] Tile already exists at {tile.position.x}, {tile.position.y}");
                return;
            }
            
            landTiles[key] = tile;
            CreateTileVisual(tile);
            
            // Remove from purchasable visuals if it was showing there
            if (purchasableVisuals.ContainsKey(key))
            {
                Destroy(purchasableVisuals[key]);
                purchasableVisuals.Remove(key);
            }
        }
        
        private void CreateTileVisual(LandTile tile)
        {
            string key = GetPositionKey(tile.position);
            Vector3 worldPos = GridToWorldPosition(tile.position);
            
            GameObject tileObj;
            
            if (tilePrefab != null)
            {
                tileObj = Instantiate(tilePrefab, worldPos, Quaternion.Euler(90, 0, 0), transform);
                tileObj.transform.localScale = Vector3.one * tileSize * 0.95f;
            }
            else
            {
                tileObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tileObj.transform.position = worldPos;
                tileObj.transform.rotation = Quaternion.Euler(90, 0, 0);
                tileObj.transform.localScale = Vector3.one * tileSize * 0.95f;
                tileObj.transform.SetParent(transform);
            }
            
            tileObj.name = $"Tile_{tile.position.x}_{tile.position.y}";
            tileObj.layer = LayerMask.NameToLayer("Grid");
            
            // Remove collider from tile visual (we use plane raycast instead)
            Collider col = tileObj.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            Renderer renderer = tileObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = tile.IsEmpty ? ownedTileMaterial : emptyTileMaterial;
            }
            
            tileVisuals[key] = tileObj;
        }
        
        private void ClearGrid()
        {
            foreach (var visual in tileVisuals.Values)
                if (visual != null) Destroy(visual);
            
            foreach (var visual in purchasableVisuals.Values)
                if (visual != null) Destroy(visual);
            
            landTiles.Clear();
            tileVisuals.Clear();
            purchasableVisuals.Clear();
        }
        
        #endregion
        
        #region Land Purchase System
        
        /// <summary>
        /// Refreshes the blue purchasable tile indicators around all owned tiles.
        /// </summary>
        public void RefreshPurchasableTiles()
        {
            // Clear old purchasable visuals
            foreach (var visual in purchasableVisuals.Values)
                if (visual != null) Destroy(visual);
            purchasableVisuals.Clear();
            
            // Get all adjacent unowned positions
            List<Position> purchasable = GetAdjacentUnownedPositions();
            
            foreach (Position pos in purchasable)
            {
                CreatePurchasableTileVisual(pos);
            }
        }
        
        private void CreatePurchasableTileVisual(Position pos)
        {
            string key = GetPositionKey(pos);
            Vector3 worldPos = GridToWorldPosition(pos);

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.position = worldPos + Vector3.up * 0.005f; // Slightly above ground
            quad.transform.rotation = Quaternion.Euler(90, 0, 0);
            quad.transform.localScale = Vector3.one * tileSize * 0.95f;
            quad.transform.SetParent(transform);
            quad.name = $"Purchasable_{pos.x}_{pos.y}";
            quad.layer = LayerMask.NameToLayer("Grid");
            
            // Remove collider (we use plane raycast)
            Collider col = quad.GetComponent<Collider>();
            if (col != null) Destroy(col);
            
            Renderer renderer = quad.GetComponent<Renderer>();
            if (renderer != null && purchasableTileMaterial != null)
            {
                renderer.material = purchasableTileMaterial;
            }
            
            purchasableVisuals[key] = quad;
        }
        
        private List<Position> GetAdjacentUnownedPositions()
        {
            // The 4 cardinal directions
            int[] dx = { 1, -1, 0,  0 };
            int[] dy = { 0,  0, 1, -1 };
            
            List<Position> result = new List<Position>();
            HashSet<string> seen = new HashSet<string>();
            
            foreach (var key in landTiles.Keys)
            {
                LandTile tile = landTiles[key];
                
                for (int i = 0; i < 4; i++)
                {
                    Position neighbour = new Position
                    {
                        x = tile.position.x + dx[i],
                        y = tile.position.y + dy[i]
                    };
                    
                    string neighbourKey = GetPositionKey(neighbour);
                    
                    // Add if not already owned and not already in our result list
                    if (!landTiles.ContainsKey(neighbourKey) && !seen.Contains(neighbourKey))
                    {
                        seen.Add(neighbourKey);
                        result.Add(neighbour);
                    }
                }
            }
            
            return result;
        }
        
        public bool IsAdjacentToOwnedTile(Position pos)
        {
            int[] dx = { 1, -1, 0,  0 };
            int[] dy = { 0,  0, 1, -1 };
            
            for (int i = 0; i < 4; i++)
            {
                string neighbourKey = GetPositionKey(pos.x + dx[i], pos.y + dy[i]);
                if (landTiles.ContainsKey(neighbourKey)) return true;
            }
            return false;
        }
        
        private void HandleLandPurchaseHover()
        {
            Position pos = GetGridPositionFromMouse();
            if (pos == null) return;
            
            string key = GetPositionKey(pos);
            
            // Pulse/tint the purchasable tile on hover
            if (purchasableVisuals.ContainsKey(key))
            {
                // Tile is purchasable - show a brighter highlight
                foreach (var kvp in purchasableVisuals)
                {
                    Renderer r = kvp.Value.GetComponent<Renderer>();
                    if (r == null) continue;
                    
                    if (kvp.Key == key)
                        r.material.color = new Color(0.3f, 0.6f, 1f, 0.9f); // Bright blue on hover
                    else if (purchasableTileMaterial != null)
                        r.material.color = purchasableTileMaterial.color; // Reset others
                }
            }
        }
        
        private void HandleLandPurchaseClick()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            
            // Don't handle if pointer is over UI
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;
            
            Position pos = GetGridPositionFromMouse();
            if (pos == null) return;
            
            string key = GetPositionKey(pos);
            
            // Check if clicking on a purchasable (unowned adjacent) tile
            if (purchasableVisuals.ContainsKey(key))
            {
                // Calculate land cost based on distance from origin
                float cost = CalculateLandCost(pos);
                Debug.Log($"[GridManager] Clicked purchasable tile at ({pos.x}, {pos.y}). Cost: Rs.{cost}");
                
                // Fire event for LandPurchaseUIManager to catch
                OnTileClickedForPurchase?.Invoke(pos, cost);
            }
        }
        
        /// <summary>
        /// Land cost increases slightly the further you are from origin.
        /// </summary>
        private float CalculateLandCost(Position pos)
        {
            // Count how many tiles we currently own
            int currentTilesCount = landTiles.Count;
            
            float baseCost = 2000f;
            // Cost increases by 20% (1.2 multiplier) for each tile owned
            float multiplier = Mathf.Pow(1.2f, currentTilesCount);
            
            return Mathf.Round(baseCost * multiplier);
        }
        
        /// <summary>
        /// Called after land is successfully purchased. Updates the grid.
        /// </summary>
        public void OnLandPurchaseSuccess(LandTile tile)
        {
            AddLandTile(tile);
            RefreshPurchasableTiles(); // Recalculate neighbours
        }
        
        #endregion
        
        #region Grid Queries
        
        public Vector3 GridToWorldPosition(Position gridPos)
        {
            return gridOrigin + new Vector3(gridPos.x * tileSize, 0f, gridPos.y * tileSize);
        }
        
        public Vector3 GridToWorldPosition(int x, int y)
        {
            return GridToWorldPosition(new Position { x = x, y = y });
        }
        
        public Position WorldToGridPosition(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - gridOrigin;
            return new Position
            {
                x = Mathf.RoundToInt(localPos.x / tileSize),
                y = Mathf.RoundToInt(localPos.z / tileSize)
            };
        }
        
        public Position GetGridPositionFromMouse()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            Plane gridPlane = new Plane(Vector3.up, Vector3.zero);
            
            if (gridPlane.Raycast(ray, out float distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                return WorldToGridPosition(hitPoint);
            }
            return null;
        }
        
        public bool IsTileOwned(Position pos) => landTiles.ContainsKey(GetPositionKey(pos));
        
        public bool IsTileEmpty(Position pos)
        {
            string key = GetPositionKey(pos);
            if (!landTiles.ContainsKey(key)) return false;
            return landTiles[key].IsEmpty;
        }
        
        public bool IsValidBuildingPlacement(Position pos) => IsTileOwned(pos) && IsTileEmpty(pos);
        
        public LandTile GetTileAt(Position pos)
        {
            string key = GetPositionKey(pos);
            return landTiles.ContainsKey(key) ? landTiles[key] : null;
        }
        
        #endregion
        
        #region Visual Feedback
        
        public void HighlightTile(Position pos, bool isValid)
        {
            ClearHighlight();
            
            string key = GetPositionKey(pos);
            
            if (!tileVisuals.ContainsKey(key))
            {
                Vector3 worldPos = GridToWorldPosition(pos);
                GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
                highlight.transform.position = worldPos + Vector3.up * 0.01f;
                highlight.transform.rotation = Quaternion.Euler(90, 0, 0);
                highlight.transform.localScale = Vector3.one * tileSize * 0.9f;
                
                Collider col = highlight.GetComponent<Collider>();
                if (col != null) Destroy(col);
                
                Renderer renderer = highlight.GetComponent<Renderer>();
                renderer.material = invalidPlacementMaterial;
                
                currentHighlightedTile = highlight;
                currentHighlightedKey = null;
                return;
            }
            
            GameObject tileObj = tileVisuals[key];
            Renderer tileRenderer = tileObj.GetComponent<Renderer>();
            if (tileRenderer != null)
                tileRenderer.material = isValid ? validPlacementMaterial : invalidPlacementMaterial;
            
            currentHighlightedTile = tileObj;
            currentHighlightedKey = key;
        }
        
        public void ClearHighlight()
        {
            if (currentHighlightedTile != null)
            {
                if (currentHighlightedKey != null && landTiles.ContainsKey(currentHighlightedKey))
                {
                    LandTile tile = landTiles[currentHighlightedKey];
                    Renderer renderer = currentHighlightedTile.GetComponent<Renderer>();
                    if (renderer != null)
                        renderer.material = tile.IsEmpty ? ownedTileMaterial : emptyTileMaterial;
                }
                else
                {
                    Destroy(currentHighlightedTile);
                }
                
                currentHighlightedTile = null;
                currentHighlightedKey = null;
            }
        }
        
        public void UpdateTileAfterBuildingPlaced(Position pos, string businessId)
        {
            string key = GetPositionKey(pos);
            if (landTiles.ContainsKey(key))
            {
                landTiles[key].business_id = businessId;
                landTiles[key].tile_type = "shop";
                
                if (tileVisuals.ContainsKey(key))
                {
                    Renderer renderer = tileVisuals[key].GetComponent<Renderer>();
                    if (renderer != null)
                        renderer.material = emptyTileMaterial;
                }
            }
        }
        
        #endregion
        
        #region Helpers
        
        private string GetPositionKey(Position pos) => $"{pos.x}_{pos.y}";
        private string GetPositionKey(int x, int y) => $"{x}_{y}";
        
        #endregion
        
        #region Public Properties
        
        public float TileSize => tileSize;
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public Vector3 GridOrigin => gridOrigin;
        
        #endregion
    }
}
