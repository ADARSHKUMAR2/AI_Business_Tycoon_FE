using UnityEngine;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Sits on each spawned store building. Detects click in Macro mode
    /// to allow player to enter. Also defines the entrance position.
    /// </summary>
    public class StoreInteractionManager : MonoBehaviour
    {
        [Header("Store Data")]
        public BusinessData BusinessData;

        [Header("Entrance")]
        [SerializeField] private Transform entrancePoint; // Place this child at the store's front door

        [Header("Interaction Prompt")]
        [SerializeField] private GameObject interactionPromptUI; // World-space canvas that shows "Tap to Enter"

        private bool isHovered = false;

        private void Start()
        {
            // Auto-create an entrance point if none assigned
            if (entrancePoint == null)
            {
                GameObject ep = new GameObject("EntrancePoint");
                ep.transform.SetParent(transform);
                // Default entrance: front-center of the building, slightly inside
                ep.transform.localPosition = new Vector3(0, 0.1f, -3f);
                entrancePoint = ep.transform;
            }

            HidePrompt();
        }

        private void OnMouseEnter()
        {
            // Only respond in Macro mode
            if (GameManager.Instance == null) return;
            if (CameraController.Instance == null) return;
            if (CameraController.Instance.CurrentMode != CameraController.CameraMode.MacroView) return;

            isHovered = true;
            ShowPrompt();
        }

        private void OnMouseExit()
        {
            isHovered = false;
            HidePrompt();
        }

        private void OnMouseDown()
        {
            // Only respond in Macro mode
            if (GameManager.Instance == null) return;
            if (CameraController.Instance == null) return;
            if (CameraController.Instance.CurrentMode != CameraController.CameraMode.MacroView) return;

            // Prevent click-through from UI
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            Debug.Log($"[StoreInteractionManager] Entering store: {BusinessData?.name}");
            GameManager.Instance.EnterStore(this);
        }

        public Vector3 GetEntrancePosition() => entrancePoint.position;

        private void ShowPrompt()
        {
            if (interactionPromptUI != null) interactionPromptUI.SetActive(true);
        }

        private void HidePrompt()
        {
            if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
        }
    }
}
