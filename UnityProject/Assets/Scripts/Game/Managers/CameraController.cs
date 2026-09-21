using UnityEngine;
using System.Collections;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Dual-mode camera controller.
    /// MacroView: Free-roaming isometric city view.
    /// MicroView: Tight follow-cam locked to the player avatar inside a store.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        public enum CameraMode { MacroView, MicroView }
        
        [Header("Camera References")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Transform cameraPivot;

        [Header("Macro View Settings")]
        [SerializeField] private float panSpeed = 50f;
        [SerializeField] private float edgePanThreshold = 10f;
        [SerializeField] private bool enableEdgePanning = true;
        [SerializeField] private float zoomSpeed = 20f;
        [SerializeField] private float minZoom = 10f;
        [SerializeField] private float maxZoom = 100f;
        [SerializeField] private float currentZoom = 60f;
        [SerializeField] private bool enableRotation = true;
        [SerializeField] private float rotationSpeed = 100f;

        [Header("Micro View Settings")]
        [SerializeField] private Vector3 microViewOffset = new Vector3(0f, 8f, -6f);
        [SerializeField] private float microViewFollowSpeed = 8f;
        [SerializeField] private float microViewZoom = 12f;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 0.8f;

        [Header("Bounds (Macro Only)")]
        [SerializeField] private bool enableBounds = true;
        [SerializeField] private float minX = -100f;
        [SerializeField] private float maxX = 100f;
        [SerializeField] private float minZ = -100f;
        [SerializeField] private float maxZ = 100f;
        [SerializeField] private float smoothSpeed = 8f;

        // State
        public CameraMode CurrentMode { get; private set; } = CameraMode.MacroView;

        public static CameraController Instance { get; private set; }
        private Transform followTarget;
        private Vector3 targetPosition;
        private float targetRotationY;
        private bool isTransitioning = false;

        // Drag
        private Vector3 dragOrigin;
        private bool isDragging = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (mainCamera == null) mainCamera = Camera.main;

            if (cameraPivot == null)
            {
                GameObject pivotObj = new GameObject("CameraPivot");
                cameraPivot = pivotObj.transform;
                mainCamera.transform.SetParent(cameraPivot);
            }

            targetPosition = cameraPivot.position;
            targetRotationY = cameraPivot.eulerAngles.y;
            SetZoom(currentZoom);
        }

        private void LateUpdate()
        {
            if (isTransitioning) return;

            if (CurrentMode == CameraMode.MacroView)
            {
                HandleKeyboardPanning();
                HandleMouseDragPanning();
                HandleEdgePanning();
                HandleZoom();
                HandleRotation();
                ApplyMacroMovement();
            }
            else
            {
                ApplyMicroFollow();
            }
        }

        #region Macro View Controls

        private void HandleKeyboardPanning()
        {
            Vector3 movement = Vector3.zero;

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    movement += Vector3.forward;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  movement += Vector3.back;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  movement += Vector3.left;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) movement += Vector3.right;

            if (movement != Vector3.zero)
            {
                Vector3 rotatedMovement = Quaternion.Euler(0, targetRotationY, 0) * movement;
                targetPosition += rotatedMovement * panSpeed * Time.deltaTime;
            }
        }

        private void HandleMouseDragPanning()
        {
            if (Input.GetMouseButtonDown(2))
            {
                dragOrigin = Input.mousePosition;
                isDragging = true;
            }

            if (Input.GetMouseButton(2) && isDragging)
            {
                Vector3 diff = Input.mousePosition - dragOrigin;
                dragOrigin = Input.mousePosition;
                Vector3 move = Quaternion.Euler(0, targetRotationY, 0) * new Vector3(-diff.x, 0, -diff.y);
                targetPosition += move * panSpeed * Time.deltaTime * 0.1f;
            }

            if (Input.GetMouseButtonUp(2)) isDragging = false;
        }

        private void HandleEdgePanning()
        {
            if (!enableEdgePanning) return;
            Vector3 movement = Vector3.zero;
            Vector3 mousePos = Input.mousePosition;

            if (mousePos.x < edgePanThreshold)                  movement += Vector3.left;
            if (mousePos.x > Screen.width - edgePanThreshold)   movement += Vector3.right;
            if (mousePos.y < edgePanThreshold)                  movement += Vector3.back;
            if (mousePos.y > Screen.height - edgePanThreshold)  movement += Vector3.forward;

            if (movement != Vector3.zero)
            {
                Vector3 rotated = Quaternion.Euler(0, targetRotationY, 0) * movement;
                targetPosition += rotated * panSpeed * Time.deltaTime;
            }
        }

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

        private void HandleRotation()
        {
            if (!enableRotation) return;
            if (Input.GetKey(KeyCode.Q)) targetRotationY -= rotationSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) targetRotationY += rotationSpeed * Time.deltaTime;
            targetRotationY = Mathf.Repeat(targetRotationY, 360f);
        }

        private void ApplyMacroMovement()
        {
            if (enableBounds)
            {
                targetPosition.x = Mathf.Clamp(targetPosition.x, minX, maxX);
                targetPosition.z = Mathf.Clamp(targetPosition.z, minZ, maxZ);
            }

            cameraPivot.position = Vector3.Lerp(cameraPivot.position, targetPosition, smoothSpeed * Time.deltaTime);

            Vector3 rot = cameraPivot.eulerAngles;
            rot.y = Mathf.LerpAngle(rot.y, targetRotationY, smoothSpeed * Time.deltaTime);
            cameraPivot.eulerAngles = rot;
        }

        #endregion

        #region Micro View Controls

        private void ApplyMicroFollow()
        {
            if (followTarget == null) return;

            // Smoothly follow the avatar
            Vector3 desiredPos = followTarget.position;
            cameraPivot.position = Vector3.Lerp(cameraPivot.position, desiredPos, microViewFollowSpeed * Time.deltaTime);
        }

        #endregion

        #region Mode Transitions

        /// <summary>
        /// Called by GameManager when player clicks "Enter Store".
        /// Smoothly swoops the camera down to follow the avatar.
        /// </summary>
        public void EnterMicroView(Transform avatarTransform)
        {
            if (isTransitioning) return;
            followTarget = avatarTransform;
            StartCoroutine(TransitionToMicroView());
        }

        /// <summary>
        /// Called by GameManager when player clicks "Leave Store".
        /// Swoops camera back up to macro view.
        /// </summary>
        public void ExitToMacroView()
        {
            if (isTransitioning) return;
            StartCoroutine(TransitionToMacroView());
        }

        private IEnumerator TransitionToMicroView()
        {
            isTransitioning = true;

            // Capture starting state
            Vector3 startPos = cameraPivot.position;
            Vector3 startLocalCamPos = mainCamera.transform.localPosition;
            Vector3 targetLocalCamPos = microViewOffset;

            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

                // Move pivot exactly to the avatar's position
                if (followTarget != null)
                    cameraPivot.position = Vector3.Lerp(startPos, followTarget.position, t);

                // Zoom camera to the shoulder/overhead view
                mainCamera.transform.localPosition = Vector3.Lerp(startLocalCamPos, targetLocalCamPos, t);

                yield return null;
            }

            // Lock in final position
            mainCamera.transform.localPosition = targetLocalCamPos;
            
            CurrentMode = CameraMode.MicroView;
            isTransitioning = false;

            Debug.Log("[CameraController] Switched to MicroView");
        }

        private IEnumerator TransitionToMacroView()
        {
            isTransitioning = true;

            // Capture starting state
            Vector3 startPos = cameraPivot.position;
            Vector3 startLocalCamPos = mainCamera.transform.localPosition;
            
            // NEW: The exact macro position we want the pivot to return to.
            // We want it to look at the store from slightly above.
            Vector3 targetPivotPos = new Vector3(startPos.x, 0f, startPos.z);
            
            // NEW: The exact local offset for the perfect isometric angle at the macro scale.
            // We set this to (0, 30, -30) in the SceneSetupEditor to handle the 10x10 tile size.
            Vector3 targetLocalCamPos = new Vector3(0, currentZoom * 0.75f, -currentZoom * 0.75f);

            float elapsed = 0f;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

                // Smoothly pull the pivot back to the ground plane
                cameraPivot.position = Vector3.Lerp(startPos, targetPivotPos, t);
                
                // Smoothly pull the camera back out to the sky
                mainCamera.transform.localPosition = Vector3.Lerp(startLocalCamPos, targetLocalCamPos, t);

                yield return null;
            }

            // Lock in final positions to prevent drifting
            cameraPivot.position = targetPivotPos;
            mainCamera.transform.localPosition = targetLocalCamPos;
            
            // Tell the pan logic that this is our new starting point
            targetPosition = targetPivotPos;
            
            followTarget = null;
            CurrentMode = CameraMode.MacroView;
            isTransitioning = false;

            Debug.Log("[CameraController] Switched to MacroView");
        }

        #endregion

        #region Public Methods

        public void FocusOnPosition(Vector3 worldPosition)
        {
            targetPosition = new Vector3(worldPosition.x, cameraPivot.position.y, worldPosition.z);
        }

        public void SetBounds(float gridMinX, float gridMaxX, float gridMinZ, float gridMaxZ, float padding = 5f)
        {
            float left = Mathf.Min(gridMinX, gridMaxX) - padding;
            float right = Mathf.Max(gridMinX, gridMaxX) + padding;
            float bottom = Mathf.Min(gridMinZ, gridMaxZ) - padding;
            float top = Mathf.Max(gridMinZ, gridMaxZ) + padding;

            minX = left;
            maxX = right;
            minZ = bottom;
            maxZ = top;
        }

        private void SetZoom(float zoom)
        {
            Vector3 localPos = mainCamera.transform.localPosition;
            localPos.z = -zoom;
            mainCamera.transform.localPosition = localPos;
        }

        #endregion
    }
}
