using UnityEngine;
using UnityEngine.EventSystems;
using AIBusinessTycoon.Managers;

namespace AIBusinessTycoon.UI
{
    /// <summary>
    /// On-screen floating joystick for mobile input.
    /// Feeds normalized direction into PlayerController every frame.
    /// </summary>
    public class JoystickController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("UI References")]
        [SerializeField] private RectTransform joystickBackground;
        [SerializeField] private RectTransform joystickHandle;

        [Header("Settings")]
        [SerializeField] private float handleRange = 80f; // Max pixel distance handle can travel

        // State
        private Vector2 inputVector = Vector2.zero;
        private Vector2 startPosition;
        private bool isDragging = false;

        private void Start()
        {
            // Store default handle center
            if (joystickHandle != null)
                startPosition = joystickHandle.anchoredPosition;
        }

        private void Update()
        {
            // Push input to PlayerController every frame while dragging
            if (isDragging && PlayerController.Instance != null)
            {
                PlayerController.Instance.SetJoystickInput(inputVector);
            }
        }

        #region Pointer Events

        public void OnPointerDown(PointerEventData eventData)
        {
            isDragging = true;
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (joystickBackground == null || joystickHandle == null) return;

            // Convert screen position to local position inside joystick background
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickBackground, eventData.position, eventData.pressEventCamera, out localPoint
            );

            // Clamp within circle
            Vector2 clampedPoint = Vector2.ClampMagnitude(localPoint, handleRange);

            // Move handle
            joystickHandle.anchoredPosition = clampedPoint;

            // Normalize to -1..1 range
            inputVector = clampedPoint / handleRange;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isDragging = false;
            inputVector = Vector2.zero;

            // Snap handle back to center
            if (joystickHandle != null)
                joystickHandle.anchoredPosition = startPosition;

            // Send zero to stop player
            if (PlayerController.Instance != null)
                PlayerController.Instance.SetJoystickInput(Vector2.zero);
        }

        #endregion

        #region Public Getters

        public Vector2 Direction => inputVector;
        public bool IsBeingUsed => isDragging;

        #endregion
    }
}
