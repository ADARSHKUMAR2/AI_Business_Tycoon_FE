using UnityEngine;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Controls the player avatar during MicroView (inside a store).
    /// Supports both WASD keyboard and on-screen mobile joystick input.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;

        [Header("Visual")]
        [SerializeField] private GameObject avatarVisual; // Assign your mesh/capsule here

        // Components
        private CharacterController characterController;
        private Camera mainCamera;

        // Input (set externally by Joystick or read from keyboard)
        private Vector2 inputDirection;

        // State
        private bool isActive = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (!isActive) return;

            ReadKeyboardInput();
            MoveAvatar();
        }

        #region Input

        private void ReadKeyboardInput()
        {
            // Keyboard input (overridden by joystick when on mobile)
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // Only override if joystick isn't providing input
            if (inputDirection.magnitude < 0.1f)
            {
                inputDirection = new Vector2(h, v);
            }
        }

        /// <summary>
        /// Called by the on-screen joystick UI component.
        /// </summary>
        public void SetJoystickInput(Vector2 direction)
        {
            inputDirection = direction;
        }

        #endregion

        #region Movement

        private void MoveAvatar()
        {
            if (inputDirection.magnitude < 0.05f)
            {
                // No input - clear joystick so keyboard can take over next frame
                inputDirection = Vector2.zero;
                return;
            }

            // Convert 2D input to 3D world direction relative to camera
            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight   = mainCamera.transform.right;

            // Flatten to horizontal plane
            camForward.y = 0;
            camRight.y   = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * inputDirection.y + camRight * inputDirection.x).normalized;

            // Move
            characterController.Move(moveDir * moveSpeed * Time.deltaTime);

            // Apply gravity
            if (!characterController.isGrounded)
                characterController.Move(Vector3.down * 9.81f * Time.deltaTime);

            // Rotate avatar to face movement direction
            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // Clear input after consuming it
            inputDirection = Vector2.zero;
        }

        #endregion

        #region Enable / Disable

        /// <summary>
        /// Called by GameManager when entering a store.
        /// Teleports avatar to entrance and activates it.
        /// </summary>
        public void ActivateAtPosition(Vector3 position)
        {
            // Ensure components are initialized even if Awake hasn't run yet
            InitializeComponents();

            // Teleport
            if (characterController != null) characterController.enabled = false;
            
            transform.position = position;
            
            if (characterController != null) characterController.enabled = true;

            // Show avatar
            if (avatarVisual != null) avatarVisual.SetActive(true);
            gameObject.SetActive(true);
            isActive = true;

            Debug.Log($"[PlayerController] Avatar activated at {position}");
        }

        /// <summary>
        /// Called by GameManager when leaving a store.
        /// Hides avatar and deactivates controls.
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
            if (avatarVisual != null) avatarVisual.SetActive(false);
            gameObject.SetActive(false);

            Debug.Log("[PlayerController] Avatar deactivated");
        }

        #endregion

        #region Public Getters

        public bool IsActive => isActive;

        #endregion
    }
}
