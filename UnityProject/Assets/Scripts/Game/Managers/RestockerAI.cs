using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace AIBusinessTycoon.Managers
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class RestockerAI : MonoBehaviour
    {
        private enum RestockerState { Idle, WalkingToSupply, WalkingToShelf, Filling }
        private RestockerState currentState = RestockerState.Idle;

        private NavMeshAgent agent;

        [Header("Settings")]
        [SerializeField] private float arrivalThreshold = 0.8f;
        [SerializeField] private float supplyLoadTime = 1.0f;   
        [SerializeField] private float fillTime = 0.5f;          
        [SerializeField] private int   carryCapacity = 5;        

        private int currentCarrying = 0;
        private InteractableShelf targetShelf;
        private Transform supplyZoneTransform;

        // Visuals
        private TextMeshProUGUI floatingLabel;
        private Transform carryPoint;  
        private GameObject boxPrefab;  
        
        // ── Local Object Pool for Carried Boxes ──
        private List<GameObject> visualBoxes = new List<GameObject>();

        private IEnumerator Start()
        {
            agent = GetComponent<NavMeshAgent>();
            SetupVisuals();

            yield return new WaitUntil(() => agent.isOnNavMesh);
            yield return new WaitForSeconds(0.5f);

            GameObject supplyZoneObj = GameObject.FindGameObjectWithTag("SupplyZone");
            if (supplyZoneObj != null)
                supplyZoneTransform = supplyZoneObj.transform;
            else
                Debug.LogWarning("[RestockerAI] No GameObject with tag 'SupplyZone' found! Restocker cannot work.");

            StartCoroutine(RestockerLoop());
        }

        private IEnumerator RestockerLoop()
        {
            while (true)
            {
                targetShelf = FindShelfNeedingRestock();

                if (targetShelf == null || supplyZoneTransform == null)
                {
                    ShowLabel("😴", Color.gray);
                    currentState = RestockerState.Idle;
                    yield return new WaitForSeconds(2f);
                    continue;
                }

                currentState = RestockerState.WalkingToSupply;
                ShowLabel("🚶", Color.white);

                Vector3 supplyDest = supplyZoneTransform.position;
                if (NavMesh.SamplePosition(supplyDest, out NavMeshHit supplyHit, 3f, NavMesh.AllAreas))
                    agent.SetDestination(supplyHit.position);
                else
                    agent.SetDestination(supplyDest);

                yield return new WaitUntil(() =>
                    !agent.pathPending && agent.remainingDistance <= arrivalThreshold);

                agent.ResetPath();
                ShowLabel("📦", Color.cyan);

                float pickUpDelay = supplyLoadTime / carryCapacity;
                while (currentCarrying < carryCapacity)
                {
                    yield return new WaitForSeconds(pickUpDelay);
                    currentCarrying++;
                    UpdateVisuals();
                }

                yield return new WaitForSeconds(supplyLoadTime);
                currentCarrying = carryCapacity;

                currentState = RestockerState.WalkingToShelf;
                ShowLabel("🚶", Color.white);

                Vector3 shelfDest = targetShelf.transform.position;
                Vector3 offset = (transform.position - shelfDest).normalized * 1.5f; 
                
                if (NavMesh.SamplePosition(shelfDest + offset, out NavMeshHit hit, 3f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
                else
                    agent.SetDestination(shelfDest);

                yield return new WaitUntil(() =>
                    !agent.pathPending && agent.remainingDistance <= arrivalThreshold);

                agent.ResetPath();
                ShowLabel("🛠️", Color.yellow);
                currentState = RestockerState.Filling;

                while (currentCarrying > 0 && targetShelf != null && targetShelf.CanAcceptStock())
                {
                    yield return new WaitForSeconds(fillTime);
                    currentCarrying--;
                    targetShelf.AddStock(1);
                    UpdateVisuals();
                }

                ShowLabel("✅", Color.green);
                yield return new WaitForSeconds(0.5f);
            }
        }

        private InteractableShelf FindShelfNeedingRestock()
        {
            InteractableShelf[] allShelves = transform.parent.GetComponentsInChildren<InteractableShelf>();
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
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(1f, 0.5f, 0.1f); 

            GameObject cp = new GameObject("CarryPoint");
            cp.transform.SetParent(transform);
            cp.transform.localPosition = new Vector3(0, 2.2f, 0.5f); 
            carryPoint = cp.transform;

            boxPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxPrefab.name = "BoxTemplate_Hidden";
            boxPrefab.transform.SetParent(transform);
            boxPrefab.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            boxPrefab.GetComponent<Renderer>().material.color = Color.yellow;
            Destroy(boxPrefab.GetComponent<Collider>());
            boxPrefab.SetActive(false);

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

        private void UpdateVisuals()
        {
            if (carryPoint == null || boxPrefab == null) return;

            // ── OPTIMIZATION: Object Pooling ──
            while (visualBoxes.Count < currentCarrying)
            {
                GameObject box = Instantiate(boxPrefab, carryPoint);
                int i = visualBoxes.Count;
                box.transform.localPosition = new Vector3(0, i * 0.45f, 0); 
                visualBoxes.Add(box);
            }

            // We never call Destroy()! We just turn them on or off.
            for (int i = 0; i < visualBoxes.Count; i++)
            {
                visualBoxes[i].SetActive(i < currentCarrying);
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
