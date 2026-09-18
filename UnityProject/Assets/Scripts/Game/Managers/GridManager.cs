using System.Collections.Generic;
using UnityEngine;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Manages the grid-based land system synced with backend.
    /// Handles tile ownership, building placement validation, and visual feedback.
    /// </summary>
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
        
        [Header("Prefabs")]
        [SerializeField] private GameObject tilePrefab;
        
        // Data structures
        private Dictionary<string, LandTile> landTiles = new Dictionary<string, LandTile>();
        private Dictionary<string, GameObject> tileVisuals = new Dictionary<string, GameObject>();
        private GameObject currentHighlightedTile;
        private string currentHighlightedKey;
        
        // Layer for raycasting
        private LayerMask gridLayer;
        
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
            gridLayer = LayerMask.GetMask("Grid");
        }
        
        #region Initialization
        
        /// <summary>
        /// Initialize grid from player data received from backend.
        /// </summary>
        public void InitializeFromPlayerData(PlayerTycoonData playerData)
        {
            Debug.Log($"[GridManager] Initializing grid for player: {playerData.player_id}");
            
            // Clear existing grid
            ClearGrid();
            
            // Check if player has land tiles in the array
            if (playerData.land_tiles == null || playerData.land_tiles.Count == 0)
            {
                Debug.Log("[GridManager] Player has no land. Creating starter tile at (0,0)");
                CreateStarterTile();
            }
            else
            {
                Debug.Log($"[GridManager] Found {playerData.land_tiles.Count} land tiles to draw.");
                
                // Draw every tile the player owns
                foreach (LandTile tile in playerData.land_tiles)
                {
                    AddLandTile(tile);
                }
            }
            
            Debug.Log($"[GridManager] Grid initialized with {landTiles.Count} tiles");
        }

        /// <summary>
        /// Create a starter tile for new players.
        /// </summary>
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
        
        /// <summary>
        /// Add a land tile and create its visual representation.
        /// </summary>
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
        }
        
        /// <summary>
        /// Create visual GameObject for a tile.
        /// </summary>
        private void CreateTileVisual(LandTile tile)
        {
            string key = GetPositionKey(tile.position);
            Vector3 worldPos = GridToWorldPosition(tile.position);
            
            GameObject tileObj;
            
            if (tilePrefab != null)
            {
                tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                // Create default quad if no prefab assigned
                tileObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                tileObj.transform.position = worldPos;
                tileObj.transform.rotation = Quaternion.Euler(90, 0, 0);
                tileObj.transform.localScale = Vector3.one * tileSize * 0.95f;
                tileObj.transform.SetParent(transform);
            }
            
            tileObj.name = $"Tile_{tile.position.x}_{tile.position.y}";
            tileObj.layer = LayerMask.NameToLayer("Grid");
            
            // Set material based on ownership
            Renderer renderer = tileObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = tile.IsEmpty ? ownedTileMaterial : emptyTileMaterial;
            }
            
            tileVisuals[key] = tileObj;
        }
        
        /// <summary>
        /// Clear all grid tiles and visuals.
        /// </summary>
        private void ClearGrid()
        {
            foreach (var visual in tileVisuals.Values)
            {
                if (visual != null)
                    Destroy(visual);
            }
            
            landTiles.Clear();
            tileVisuals.Clear();
        }
        
        #endregion
        
        #region Grid Queries
        
        /// <summary>
        /// Convert grid position to world position.
        /// </summary>
        public Vector3 GridToWorldPosition(Position gridPos)
        {
            return gridOrigin + new Vector3(
                gridPos.x * tileSize,
                0f,
                gridPos.y * tileSize
            );
        }
        
        /// <summary>
        /// Convert grid position to world position (int overload).
        /// </summary>
        public Vector3 GridToWorldPosition(int x, int y)
        {
            return GridToWorldPosition(new Position { x = x, y = y });
        }
        
        /// <summary>
        /// Convert world position to grid position.
        /// </summary>
        public Position WorldToGridPosition(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - gridOrigin;
            return new Position
            {
                x = Mathf.RoundToInt(localPos.x / tileSize),
                y = Mathf.RoundToInt(localPos.z / tileSize)
            };
        }
        
        /// <summary>
        /// Get grid position under mouse cursor using raycast.
        /// </summary>
        public Position GetGridPositionFromMouse()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            // Raycast against horizontal plane at y=0
            Plane gridPlane = new Plane(Vector3.up, Vector3.zero);
            
            if (gridPlane.Raycast(ray, out float distance))
            {
                Vector3 hitPoint = ray.GetPoint(distance);
                return WorldToGridPosition(hitPoint);
            }
            
            return null;
        }
        
        /// <summary>
        /// Check if player owns a tile at the given position.
        /// </summary>
        public bool IsTileOwned(Position pos)
        {
            string key = GetPositionKey(pos);
            return landTiles.ContainsKey(key);
        }
        
        /// <summary>
        /// Check if a tile is empty (owned but no building).
        /// </summary>
        public bool IsTileEmpty(Position pos)
        {
            string key = GetPositionKey(pos);
            
            if (!landTiles.ContainsKey(key))
                return false;
            
            return landTiles[key].IsEmpty;
        }
        
        /// <summary>
        /// Check if a tile is valid for building placement.
        /// </summary>
        public bool IsValidBuildingPlacement(Position pos)
        {
            return IsTileOwned(pos) && IsTileEmpty(pos);
        }
        
        /// <summary>
        /// Get land tile at position.
        /// </summary>
        public LandTile GetTileAt(Position pos)
        {
            string key = GetPositionKey(pos);
            return landTiles.ContainsKey(key) ? landTiles[key] : null;
        }
        
        #endregion
        
        #region Visual Feedback
        
        /// <summary>
        /// Highlight a tile with valid/invalid placement color.
        /// </summary>
        public void HighlightTile(Position pos, bool isValid)
        {
            // Clear previous highlight
            ClearHighlight();
            
            string key = GetPositionKey(pos);
            
            if (!tileVisuals.ContainsKey(key))
            {
                // Create temporary highlight for unowned tiles
                Vector3 worldPos = GridToWorldPosition(pos);
                GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Quad);
                highlight.transform.position = worldPos + Vector3.up * 0.01f;
                highlight.transform.rotation = Quaternion.Euler(90, 0, 0);
                highlight.transform.localScale = Vector3.one * tileSize * 0.9f;
                
                Renderer renderer = highlight.GetComponent<Renderer>();
                renderer.material = invalidPlacementMaterial;
                
                currentHighlightedTile = highlight;
                currentHighlightedKey = null;
                return;
            }
            
            GameObject tileObj = tileVisuals[key];
            Renderer tileRenderer = tileObj.GetComponent<Renderer>();
            
            if (tileRenderer != null)
            {
                tileRenderer.material = isValid ? validPlacementMaterial : invalidPlacementMaterial;
            }
            
            currentHighlightedTile = tileObj;
            currentHighlightedKey = key;
        }
        
        /// <summary>
        /// Clear tile highlight and restore original material.
        /// </summary>
        public void ClearHighlight()
        {
            if (currentHighlightedTile != null)
            {
                if (currentHighlightedKey != null && landTiles.ContainsKey(currentHighlightedKey))
                {
                    // Restore original material
                    LandTile tile = landTiles[currentHighlightedKey];
                    Renderer renderer = currentHighlightedTile.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = tile.IsEmpty ? ownedTileMaterial : emptyTileMaterial;
                    }
                }
                else
                {
                    // Destroy temporary highlight
                    Destroy(currentHighlightedTile);
                }
                
                currentHighlightedTile = null;
                currentHighlightedKey = null;
            }
        }
        
        /// <summary>
        /// Update tile visual after business is placed.
        /// </summary>
        public void UpdateTileAfterBuildingPlaced(Position pos, string businessId)
        {
            string key = GetPositionKey(pos);
            
            if (landTiles.ContainsKey(key))
            {
                landTiles[key].business_id = businessId;
                landTiles[key].tile_type = "shop";
                
                // Update visual material
                if (tileVisuals.ContainsKey(key))
                {
                    Renderer renderer = tileVisuals[key].GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = emptyTileMaterial;
                    }
                }
            }
        }
        
        #endregion
        
        #region Helpers
        
        private string GetPositionKey(Position pos)
        {
            return $"{pos.x}_{pos.y}";
        }
        
        private string GetPositionKey(int x, int y)
        {
            return $"{x}_{y}";
        }
        
        #endregion
        
        #region Public Properties
        
        public float TileSize => tileSize;
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public Vector3 GridOrigin => gridOrigin;
        
        #endregion
    }
}
