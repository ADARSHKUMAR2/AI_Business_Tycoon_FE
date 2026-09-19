using UnityEngine;
using UnityEngine.EventSystems;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Attached to AI Employees (Cashier, Restocker, Cleaner).
    /// Detects clicks, opens the UI, and physically applies stats to the 3D Agent.
    /// </summary>
    public class EmployeeInteractionManager : MonoBehaviour
    {
        public Employee employeeData;
        private string businessId;

        [Header("Hover Visuals")]
        [SerializeField] private Material outlineMaterial; 
        private Renderer[] renderers;

        private void Start()
        {
            renderers = GetComponentsInChildren<Renderer>();
        }

        public void Initialize(Employee data, string bId)
        {
            employeeData = data;
            businessId = bId;
            ApplyPhysicalStats(); // Apply stats immediately upon spawning
        }

        /// <summary>
        /// Reads the backend stats and makes the visual 3D agent walk faster / carry more.
        /// </summary>
        public void ApplyPhysicalStats()
        {
            if (employeeData == null) return;

            // 1. Map Backend Speed (1-100) to Unity NavMesh Speed (e.g., 3.0 to 8.0)
            var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null)
            {
                float worldSpeed = 3.0f + (employeeData.stats.speed / 100f) * 5.0f;
                agent.speed = worldSpeed;
            }

            // 2. Map Carry Capacity if this is a Restocker
            var restocker = GetComponent<RestockerAI>();
            if (restocker != null)
            {
                // Safely update the private field via reflection
                var field = typeof(RestockerAI).GetField("carryCapacity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(restocker, employeeData.stats.carry_capacity);
                }
            }
        }

        private void OnMouseEnter()
        {
            if (CameraController.Instance == null || CameraController.Instance.CurrentMode != CameraController.CameraMode.MicroView) return;
        }

        private void OnMouseExit()
        {
        }

        private void OnMouseDown()
        {
            if (CameraController.Instance == null || CameraController.Instance.CurrentMode != CameraController.CameraMode.MicroView) return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            if (employeeData != null && !string.IsNullOrEmpty(businessId))
            {
                // Pass THIS script instead of just the data, so the UI can tell us when the upgrade completes
                UI.EmployeeUpgradeUIManager.Instance?.OpenUpgradeUI(this, businessId);
            }
        }
    }
}
