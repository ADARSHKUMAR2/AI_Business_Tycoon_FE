using UnityEngine;
using System.Collections.Generic;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.Managers
{
    public class StoreInteractionManager : MonoBehaviour
    {
        [Header("Store Data")]
        public BusinessData BusinessData;

        [Header("Entrance")]
        [SerializeField] private Transform entrancePoint;

        [Header("Interaction Prompt")]
        [SerializeField] private GameObject interactionPromptUI;

        private bool isHovered = false;
        private bool employeesSpawned = false; // Prevent double-spawning

        private void Awake()
        {
            if (entrancePoint == null)
            {
                GameObject ep = new GameObject("EntrancePoint");
                ep.transform.SetParent(transform);
                ep.transform.localPosition = new Vector3(0, 0.1f, -3f);
                entrancePoint = ep.transform;
            }

            HidePrompt();
        }

        // --- Spawns employees saved in the backend ---
        public void SpawnSavedEmployees(GameObject cashierPrefab, GameObject restockerPrefab)
        {
            if (employeesSpawned || BusinessData == null || BusinessData.employees == null) return;

            foreach (Employee emp in BusinessData.employees)
            {
                GameObject prefab = null;
                Vector3 spawnOffset = Vector3.zero;

                if (emp.role == "cashier")
                {
                    prefab = cashierPrefab;
                    spawnOffset = new Vector3(2f, 0.5f, 0f);
                }
                else if (emp.role == "restocker")
                {
                    prefab = restockerPrefab;
                    spawnOffset = new Vector3(-2f, 0.5f, 0f);
                }

                if (prefab != null)
                {
                    Vector3 spawnPos = GetEntrancePosition() + spawnOffset;
                    GameObject ai = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
                    ai.name = $"{emp.role}_{emp.employee_id.Substring(0, 4)}";
                    Debug.Log($"[StoreInteractionManager] Respawned saved {emp.role}");
                }
            }

            employeesSpawned = true;
        }

        private void OnMouseEnter()
        {
            if (GameManager.Instance == null || CameraController.Instance == null) return;
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
            if (GameManager.Instance == null || CameraController.Instance == null) return;
            if (CameraController.Instance.CurrentMode != CameraController.CameraMode.MacroView) return;

            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

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
