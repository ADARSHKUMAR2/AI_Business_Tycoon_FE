using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using TMPro;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Phase 2: AI Restocker employee.
    /// State Machine: Idle -> WalkingToSupply -> WalkingToShelf -> Filling
    /// Continuously finds the emptiest shelf, walks to the supply zone, 
    /// loads up, then restocks the shelf. Loops forever.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class RestockerAI : MonoBehaviour
    {
        private enum RestockerState { Idle, WalkingToSupply, WalkingToShelf, Filling }
        private RestockerState currentState = RestockerState.Idle;

        private NavMeshAgent agent;

        [Header("Settings")]
        [SerializeField] private float arrivalThreshold = 0.8f;
        [SerializeField] private float supplyLoadTime = 1.0f;   // Time spent at supply zone
        [SerializeField] private float fillTime = 0.5f;          // Time to fill one unit on shelf
        [SerializeField] private int   carryCapacity = 5;        // How many units carried per trip

        private int currentCarrying = 0;
        private InteractableShelf targetShelf;
        private Transform supplyZoneTransform;

        // Visuals
        private TextMeshProUGUI floatingLabel;
        private Transform carryPoint;  
        private GameObject boxPrefab;  

        private IEnumerator Start()
        {
            agent = GetComponent<NavMeshAgent>();
            SetupVisuals();

            yield return new WaitUntil(() => agent.isOnNavMesh);
            yield return new WaitForSeconds(0.5f);

            // Find the supply zone by tag
            GameObject supplyZoneObj = GameObject.FindGameObjectWithTag("SupplyZone");
            if (supplyZoneObj != null)
                supplyZoneTransform = supplyZoneObj.transform;
            else
                Debug.LogWarning("[RestockerAI] No GameObject with tag 'SupplyZone' found! Restocker cannot work.");

            // Begin the main loop
            StartCoroutine(RestockerLoop());
        }

        /// <summary>
        /// The core autonomous loop that runs forever.
        /// </summary>
        private IEnumerator RestockerLoop()
        {
            while (true)
            {
                // === STEP 1: Find the emptiest shelf ===
                targetShelf = FindShelfNeedingRestock();

                if (targetShelf == null || supplyZoneTransform == null)
                {
                    // Nothing to do — rest for a moment then check again
                    ShowLabel("😴", Color.gray);
                    currentState = RestockerState.Idle;
                    yield return new WaitForSeconds(2f);
                    continue;
                }

                // === STEP 2: Walk to Supply Zone ===
                currentState = RestockerState.WalkingToSupply;
                ShowLabel("🚶", Color.white);

                // Sample NavMesh near the supply zone
                Vector3 supplyDest = supplyZoneTransform.position;
                if (NavMesh.SamplePosition(supplyDest, out NavMeshHit supplyHit, 3f, NavMesh.AllAreas))
                    agent.SetDestination(supplyHit.position);
                else
                    agent.SetDestination(supplyDest);

                yield return new WaitUntil(() =>
                    !agent.pathPending && agent.remainingDistance <= arrivalThreshold);

                // === STEP 3: Load up at supply zone ===
                agent.ResetPath();
                ShowLabel("📦", Color.cyan);

                // Pick up items one by one visually
                float pickUpDelay = supplyLoadTime / carryCapacity;
                while (currentCarrying < carryCapacity)
                {
                    yield return new WaitForSeconds(pickUpDelay);
                    currentCarrying++;
                    UpdateVisuals();
                }

                yield return new WaitForSeconds(supplyLoadTime);
                currentCarrying = carryCapacity;

                // === STEP 4: Walk to the target Shelf ===
                currentState = RestockerState.WalkingToShelf;
                ShowLabel("🚶", Color.white);

                // Sample a walkable position near the shelf
                Vector3 shelfDest = targetShelf.transform.position + new Vector3(1.5f, 0, 0);
                if (NavMesh.SamplePosition(shelfDest, out NavMeshHit shelfHit, 3f, NavMesh.AllAreas))
                    agent.SetDestination(shelfHit.position);
                else
                    agent.SetDestination(targetShelf.transform.position);

                yield return new WaitUntil(() =>
                    !agent.pathPending && agent.remainingDistance <= arrivalThreshold);

                // Face the shelf
                agent.ResetPath();
                Vector3 lookDir = (targetShelf.transform.position - transform.position);
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                    transform.rotation = Quaternion.LookRotation(lookDir);

                // === STEP 5: Fill the shelf ===
                currentState = RestockerState.Filling;
                ShowLabel("🔄", Color.green);

                while (currentCarrying > 0 && targetShelf != null && targetShelf.CanAcceptStock())
                {
                    yield return new WaitForSeconds(fillTime);
                    targetShelf.AddStock(1);
                    currentCarrying--;
                    UpdateVisuals();
                }

                // Brief pause before next loop
                ShowLabel("✅", Color.green);
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>
        /// Finds the InteractableShelf in the scene with the lowest current stock.
        /// Only returns a shelf that actually needs restocking (below max capacity).
        /// </summary>
        private InteractableShelf FindShelfNeedingRestock()
        {
            InteractableShelf[] allShelves = FindObjectsOfType<InteractableShelf>();
            InteractableShelf emptiest = null;
            int lowestStock = int.MaxValue;

            foreach (var shelf in allShelves)
            {
                if (shelf.CanAcceptStock() && shelf.currentStock < lowestStock)
                {
                    lowestStock = shelf.currentStock;
                    emptiest = shelf;
                }
            }
            return emptiest;
        }

        #region Visuals

        private void SetupVisuals()
        {
            // Color the capsule orange to distinguish from customers and cashiers
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(1f, 0.5f, 0.1f); // Orange

            // --- Setup Carry Point and Box Prefab ---
            GameObject cp = new GameObject("CarryPoint");
            cp.transform.SetParent(transform);
            cp.transform.localPosition = new Vector3(0, 2.2f, 0.5f); // Above head, slightly forward
            carryPoint = cp.transform;

            boxPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxPrefab.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            boxPrefab.GetComponent<Renderer>().material.color = Color.yellow;
            Destroy(boxPrefab.GetComponent<Collider>());
            boxPrefab.SetActive(false);
            // -----------------------------------------------


            // Floating label
            GameObject canvasObj = new GameObject("RestockerCanvas");
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
        }

        // --- Visual Box Stacking ---
        private void UpdateVisuals()
        {
            if (carryPoint == null || boxPrefab == null) return;

            // Clear current visuals
            foreach (Transform child in carryPoint)
            {
                Destroy(child.gameObject);
            }

            // Stack new boxes
            for (int i = 0; i < currentCarrying; i++)
            {
                GameObject box = Instantiate(boxPrefab, carryPoint);
                box.SetActive(true);
                box.transform.localPosition = new Vector3(0, i * 0.45f, 0); // Stack upwards
            }
        }

        private void ShowLabel(string emoji, Color color)
        {
            if (floatingLabel == null) return;
            floatingLabel.text = emoji;
            floatingLabel.color = color;
        }

        private void LateUpdate()
        {
            if (floatingLabel != null && Camera.main != null)
                floatingLabel.transform.parent.rotation = Camera.main.transform.rotation;
        }

        #endregion
    }
}
