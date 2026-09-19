using UnityEngine;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Renders grid lines for visual feedback.
    /// Uses LineRenderer for lightweight wireframe grid display.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class GridVisualizer : MonoBehaviour
    {
        [Header("Grid Visual Settings")]
        [SerializeField] private bool showGrid = true;
        [SerializeField] private Color gridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        [SerializeField] private float lineWidth = 0.02f;
        [SerializeField] private int renderDistance = 15;
        
        private LineRenderer lineRenderer;
        private GridManager gridManager;
        
        private void Awake()
        {
            lineRenderer = GetComponent<LineRenderer>();
            ConfigureLineRenderer();
        }
        
        private void Start()
        {
            gridManager = GridManager.Instance;
            
            if (gridManager == null)
            {
                Debug.LogError("[GridVisualizer] GridManager instance not found!");
                enabled = false;
                return;
            }
            
            if (showGrid)
            {
                DrawGrid();
            }
        }
        
        private void ConfigureLineRenderer()
        {
            lineRenderer.startColor = gridColor;
            lineRenderer.endColor = gridColor;
            lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.receiveShadows = false;
        }
        
        private void DrawGrid()
        {
            if (gridManager == null) return;

            int gridSize = renderDistance;
            float tileSize = gridManager.TileSize;
            Vector3 origin = gridManager.GridOrigin;

            int horizontalLines = gridSize * 2 + 1;
            int verticalLines = gridSize * 2 + 1;
            int totalPoints = (horizontalLines + verticalLines) * 2;

            lineRenderer.positionCount = totalPoints;

            int index = 0;
            
            // ── FIXED: Shift lines by half a tile so they draw borders, not centers ──
            float offset = tileSize / 2f;

            // Draw horizontal lines
            for (int z = -gridSize; z <= gridSize; z++)
            {
                float zPos = (z * tileSize) - offset;
                Vector3 start = origin + new Vector3((-gridSize * tileSize) - offset, 0.01f, zPos);
                Vector3 end   = origin + new Vector3((gridSize * tileSize) - offset, 0.01f, zPos);

                lineRenderer.SetPosition(index++, start);
                lineRenderer.SetPosition(index++, end);
            }

            // Draw vertical lines
            for (int x = -gridSize; x <= gridSize; x++)
            {
                float xPos = (x * tileSize) - offset;
                Vector3 start = origin + new Vector3(xPos, 0.01f, (-gridSize * tileSize) - offset);
                Vector3 end   = origin + new Vector3(xPos, 0.01f, (gridSize * tileSize) - offset);

                lineRenderer.SetPosition(index++, start);
                lineRenderer.SetPosition(index++, end);
            }
        }

        
        /// <summary>
        /// Toggle grid visibility.
        /// </summary>
        public void ToggleGrid()
        {
            showGrid = !showGrid;
            lineRenderer.enabled = showGrid;
        }
        
        /// <summary>
        /// Set grid visibility.
        /// </summary>
        public void SetGridVisibility(bool visible)
        {
            showGrid = visible;
            lineRenderer.enabled = visible;
        }
    }
}
