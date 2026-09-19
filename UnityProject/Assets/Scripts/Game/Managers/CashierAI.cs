using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using TMPro;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Spawns, pathfinds to the CheckoutCounter's register position,
    /// and permanently stands there, allowing the queue to process automatically.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class CashierAI : MonoBehaviour
    {
        private enum CashierState { WalkingToRegister, Working }
        private CashierState currentState = CashierState.WalkingToRegister;

        private NavMeshAgent agent;
        private CheckoutCounter assignedCounter;

        [Header("Settings")]
        [SerializeField] private float arrivalThreshold = 0.6f;

        // Visuals
        private TextMeshProUGUI floatingLabel;

        private IEnumerator Start()
        {
            agent = GetComponent<NavMeshAgent>();

            // Build the floating label above the head
            SetupVisuals();

            // Wait for NavMesh to be ready
            yield return new WaitUntil(() => agent.isOnNavMesh);
            yield return new WaitForSeconds(0.2f);

            // Find the CheckoutCounter in the same building (parent)
            // search the parent's children instead of just the parent
            assignedCounter = transform.parent.GetComponentInChildren<CheckoutCounter>();

            if (assignedCounter == null)
            {
                Debug.LogError("[CashierAI] Could not find a CheckoutCounter! Cashier has nowhere to go.");
                yield break;
            }

            // Walk to register
            Vector3 destination = assignedCounter.GetRegisterWorldPosition();
            agent.SetDestination(destination);

            ShowLabel("🚶", Color.white);
            Debug.Log("[CashierAI] Walking to register...");
        }

        private void Update()
        {
            if (currentState != CashierState.WalkingToRegister) return;
            if (assignedCounter == null) return;
            if (agent == null || !agent.isOnNavMesh) return;

            // Check if we've arrived at the register
            if (!agent.pathPending && agent.remainingDistance <= arrivalThreshold)
            {
                OnArrivedAtRegister();
            }
        }

        private void OnArrivedAtRegister()
        {
            currentState = CashierState.Working;

            // Stop moving — plant feet here permanently
            agent.ResetPath();
            agent.isStopped = true;

            // Face the customer queue (opposite direction from behind the counter)
            transform.rotation = Quaternion.LookRotation(-assignedCounter.transform.forward);

            // Tell the counter a cashier is now on duty
            assignedCounter.SetCashierPresent(true);

            ShowLabel("💰", Color.yellow);
            Debug.Log("[CashierAI] Arrived at register. Now processing queue automatically!");
        }

        private void OnDestroy()
        {
            // Clean up: tell the counter there's no cashier anymore
            if (assignedCounter != null)
                assignedCounter.SetCashierPresent(false);
        }

        #region Visuals

        private void SetupVisuals()
        {
            // Color the capsule blue to distinguish from customers
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.2f, 0.4f, 1f); // Blue

            // Floating label above head
            GameObject canvasObj = new GameObject("CashierCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, 2.2f, 0);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 80);
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            GameObject textObj = new GameObject("LabelText");
            textObj.transform.SetParent(canvasObj.transform);

            floatingLabel = textObj.AddComponent<TextMeshProUGUI>();
            floatingLabel.fontSize = 40;
            floatingLabel.alignment = TextAlignmentOptions.Center;
            floatingLabel.fontStyle = FontStyles.Bold;
            floatingLabel.outlineWidth = 0.2f;
            floatingLabel.outlineColor = new Color(0, 0, 0, 0.8f);

            RectTransform rect = textObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 80);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            floatingLabel.text = "";
        }

        private void ShowLabel(string emoji, Color color)
        {
            if (floatingLabel == null) return;
            floatingLabel.text = emoji;
            floatingLabel.color = color;
        }

        private void LateUpdate()
        {
            // Always face the floating label towards the camera
            if (floatingLabel != null && Camera.main != null)
                floatingLabel.transform.parent.rotation = Camera.main.transform.rotation;
        }

        #endregion
    }
}
