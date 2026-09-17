using UnityEngine;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Handles isometric camera movement with pan, zoom, and rotation.
    /// Optimized for tycoon-style games with grid-based building placement.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Camera mainCamera;
        [Tooltip("The pivot point the camera rotates around")]
        [SerializeField] private Transform cameraPivot;
        
        [Header("Movement Settings")]
        [SerializeField] private float panSpeed = 20f;
        [SerializeField] private float edgePanThreshold = 10f;
        [SerializeField] private bool enableEdgePanning = true;
        [SerializeField] private bool enableKeyboardPanning = true;
        [SerializeField] private bool enableMouseDragPanning = true;
        
        [Header("Zoom Settings")]
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float minZoom = 5f;
        [SerializeField] private float maxZoom = 30f;
        [SerializeField] private float currentZoom = 15f;
        
        [Header("Rotation Settings")]
        [SerializeField] private bool enableRotation = true;
        [SerializeField] private float rotationSpeed = 100f;
        [SerializeField] private float[] snapAngles = { 0f, 90f, 180f, 270f };
        
        [Header("Bounds Settings")]
        [SerializeField] private bool enableBounds = true;
        [SerializeField] private float minX = -20f;
        [SerializeField] private float maxX = 20f;
        [SerializeField] private float minZ = -20f;
        [SerializeField] private float maxZ = 20f;
        
        [Header("Smoothing")]
        [SerializeField] private float smoothSpeed = 5f;
        
        // Private state
        private Vector3 dragOrigin;
        private bool isDragging = false;
        private Vector3 targetPosition;
        private float targetRotationY;
        
        private void Awake()
        {
            // Auto-assign camera if not set
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
            
            // Create pivot if not exists
            if (cameraPivot == null)
            {
                GameObject pivotObj = new GameObject("CameraPivot");
                cameraPivot = pivotObj.transform;
                mainCamera.transform.SetParent(cameraPivot);
            }
            
            // Initialize target position
            targetPosition = cameraPivot.position;
            targetRotationY = cameraPivot.eulerAngles.y;
            
            // Set initial zoom
            SetZoom(currentZoom);
        }
        
        private void LateUpdate()
        {
            HandleKeyboardPanning();
            HandleMouseDragPanning();
            HandleEdgePanning();
            HandleZoom();
            HandleRotation();
            
            // Apply smooth movement
            ApplySmoothMovement();
        }
        
        #region Panning
        
        private void HandleKeyboardPanning()
        {
            if (!enableKeyboardPanning) return;
            
            Vector3 movement = Vector3.zero;
            
            // WASD / Arrow Keys
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                movement += Vector3.forward;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                movement += Vector3.back;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                movement += Vector3.left;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                movement += Vector3.right;
            
            if (movement != Vector3.zero)
            {
                // Move relative to camera rotation
                Vector3 rotatedMovement = Quaternion.Euler(0, targetRotationY, 0) * movement;
                targetPosition += rotatedMovement * panSpeed * Time.deltaTime;
            }
        }
        
        private void HandleMouseDragPanning()
        {
            if (!enableMouseDragPanning) return;
            
            // Middle mouse button drag
            if (Input.GetMouseButtonDown(2))
            {
                dragOrigin = Input.mousePosition;
                isDragging = true;
            }
            
            if (Input.GetMouseButton(2) && isDragging)
            {
                Vector3 difference = Input.mousePosition - dragOrigin;
                dragOrigin = Input.mousePosition;
                
                // Convert screen space to world space movement
                Vector3 movement = new Vector3(-difference.x, 0, -difference.y);
                movement = Quaternion.Euler(0, targetRotationY, 0) * movement;
                targetPosition += movement * panSpeed * Time.deltaTime * 0.1f;
            }
            
            if (Input.GetMouseButtonUp(2))
            {
                isDragging = false;
            }
        }
        
        private void HandleEdgePanning()
        {
            if (!enableEdgePanning) return;
            
            Vector3 movement = Vector3.zero;
            Vector3 mousePos = Input.mousePosition;
            
            // Check screen edges
            if (mousePos.x < edgePanThreshold)
                movement += Vector3.left;
            if (mousePos.x > Screen.width - edgePanThreshold)
                movement += Vector3.right;
            if (mousePos.y < edgePanThreshold)
                movement += Vector3.back;
            if (mousePos.y > Screen.height - edgePanThreshold)
                movement += Vector3.forward;
            
            if (movement != Vector3.zero)
            {
                Vector3 rotatedMovement = Quaternion.Euler(0, targetRotationY, 0) * movement;
                targetPosition += rotatedMovement * panSpeed * Time.deltaTime;
            }
        }
        
        #endregion
        
        #region Zoom
        
        private void HandleZoom()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            
            if (Mathf.Abs(scroll) > 0.01f)
            {
                currentZoom -= scroll * zoomSpeed;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
                SetZoom(currentZoom);
            }
        }
        
        private void SetZoom(float zoom)
        {
            // Move camera closer/further from pivot
            Vector3 localPos = mainCamera.transform.localPosition;
            localPos.z = -zoom;
            mainCamera.transform.localPosition = localPos;
        }
        
        #endregion
        
        #region Rotation
        
        private void HandleRotation()
        {
            if (!enableRotation) return;
            
            // Q/E keys for rotation
            if (Input.GetKey(KeyCode.Q))
            {
                targetRotationY -= rotationSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.E))
            {
                targetRotationY += rotationSpeed * Time.deltaTime;
            }
            
            // Snap to 90-degree angles with R key
            if (Input.GetKeyDown(KeyCode.R))
            {
                SnapToNearestAngle();
            }
            
            // Keep rotation in 0-360 range
            targetRotationY = Mathf.Repeat(targetRotationY, 360f);
        }
        
        private void SnapToNearestAngle()
        {
            float currentAngle = targetRotationY;
            float nearestAngle = snapAngles[0];
            float minDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, nearestAngle));
            
            foreach (float angle in snapAngles)
            {
                float difference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, angle));
                if (difference < minDifference)
                {
                    minDifference = difference;
                    nearestAngle = angle;
                }
            }
            
            targetRotationY = nearestAngle;
        }
        
        #endregion
        
        #region Apply Movement
        
        private void ApplySmoothMovement()
        {
            // Apply bounds
            if (enableBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
                targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
            }
            
            // Smooth position
            cameraPivot.position = Vector3.Lerp(
                cameraPivot.position,
                targetPosition,
                smoothSpeed * Time.deltaTime
            );
            
            // Smooth rotation
            Vector3 currentRotation = cameraPivot.eulerAngles;
            currentRotation.y = Mathf.LerpAngle(
                currentRotation.y,
                targetRotationY,
                smoothSpeed * Time.deltaTime
            );
            cameraPivot.eulerAngles = currentRotation;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Focus camera on a specific world position.
        /// </summary>
        public void FocusOnPosition(Vector3 worldPosition, float duration = 0.5f)
        {
            targetPosition = new Vector3(worldPosition.x, cameraPivot.position.y, worldPosition.z);
        }
        
        /// <summary>
        /// Set camera bounds dynamically based on grid size.
        /// </summary>
        public void SetBounds(float gridMinX, float gridMaxX, float gridMinZ, float gridMaxZ, float padding = 5f)
        {
            minX = gridMinX - padding;
            maxX = gridMaxX + padding;
            minZ = gridMinZ - padding;
            maxZ = gridMaxZ + padding;
        }
        
        #endregion
    }
}
