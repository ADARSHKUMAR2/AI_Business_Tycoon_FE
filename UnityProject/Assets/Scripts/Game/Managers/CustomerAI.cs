using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using AIBusinessTycoon.Data;
using TMPro; 

namespace AIBusinessTycoon.Managers
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class CustomerAI : MonoBehaviour
    {
        private enum CustomerState { Initializing, WalkingToStore, Shopping, WalkingToCheckout, WaitingInLine, Leaving }
        
        private CustomerState currentState = CustomerState.Initializing;
        private NavMeshAgent agent;
        private StoreInteractionManager targetStore;
        
        [Header("Visuals")]
        private Transform carryPoint;
        private GameObject carriedItemVisual;
        private TextMeshProUGUI floatingEmoji;
        
        private InteractableShelf targetShelf;
        private bool hasItem = false;

        private IEnumerator Start()
        {
            agent = GetComponent<NavMeshAgent>();
            
            SetupVisuals();

            yield return new WaitUntil(() => agent.isOnNavMesh);
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
            
            FindRandomStore();
        }

                private void SetupVisuals()
        {
            // 1. Create a point to carry the box (in front of them)
            GameObject cp = new GameObject("CarryPoint");
            cp.transform.SetParent(transform);
            cp.transform.localPosition = new Vector3(0, 0.6f, 0.5f); // Chest height, slightly forward
            carryPoint = cp.transform;

            // 2. Create the box they will carry (hidden by default)
            carriedItemVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carriedItemVisual.transform.SetParent(carryPoint);
            carriedItemVisual.transform.localPosition = Vector3.zero;
            carriedItemVisual.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            carriedItemVisual.GetComponent<Renderer>().material.color = Color.red; // Matches the shelf items
            Destroy(carriedItemVisual.GetComponent<Collider>());
            carriedItemVisual.SetActive(false);

            // 3. Create floating emoji UI (for angry/happy faces)
            GameObject canvasObj = new GameObject("EmojiCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, 1.8f, 0); // Above head
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // FIXED SCALING: Match the same tiny scale we used for shelves
            canvasObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            GameObject textObj = new GameObject("EmojiText");
            textObj.transform.SetParent(canvasObj.transform);
            floatingEmoji = textObj.AddComponent<TextMeshProUGUI>();
            
            // Use a reasonable font size inside the scaled-down canvas
            floatingEmoji.fontSize = 50;
            floatingEmoji.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(100, 100);
            textRect.localPosition = Vector3.zero;
            textRect.localRotation = Quaternion.identity;
            textRect.localScale = Vector3.one;
            
            floatingEmoji.text = ""; // Hidden initially
        }


        private void FindRandomStore()
        {
            StoreInteractionManager[] allStores = FindObjectsOfType<StoreInteractionManager>();
            
            if (allStores.Length == 0)
            {
                Leave();
                return;
            }

            targetStore = allStores[Random.Range(0, allStores.Length)];
            currentState = CustomerState.WalkingToStore;
            agent.SetDestination(targetStore.GetEntrancePosition());
        }

        private void Update()
        {
            if (currentState == CustomerState.Initializing || !agent.isOnNavMesh) return;

            // Make the emoji billboard (always face camera)
            if (floatingEmoji.text != "")
            {
                floatingEmoji.transform.parent.rotation = Camera.main.transform.rotation;
            }

            switch (currentState)
            {
                case CustomerState.WalkingToStore:
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        StartCoroutine(ShopAroundRoutine());
                    }
                    break;
                    
                case CustomerState.WalkingToCheckout:
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        StartCoroutine(CheckoutRoutine());
                    }
                    break;

                case CustomerState.Leaving:
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                    {
                        CustomerSpawner.Instance.RemoveCustomer(gameObject);
                    }
                    break;
            }
        }

        private IEnumerator ShopAroundRoutine()
        {
            currentState = CustomerState.Shopping;
            
            // 1. Find all shelves in the target store
            InteractableShelf[] shelves = targetStore.GetComponentsInChildren<InteractableShelf>();
            
            if (shelves.Length == 0)
            {
                // No shelves? Angry leave.
                ShowEmoji("😠", Color.red);
                Leave();
                yield break;
            }

            // 2. Pick a random shelf
            targetShelf = shelves[Random.Range(0, shelves.Length)];
            
            // 3. Walk to the shelf
            Vector3 targetPos = targetShelf.transform.position + new Vector3(1.5f, 0, 0); // Stand next to it
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            
            // Wait to arrive
            yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f);
            
            // Look at the shelf
            transform.LookAt(new Vector3(targetShelf.transform.position.x, transform.position.y, targetShelf.transform.position.z));
            
            // Wait a second to "browse"
            yield return new WaitForSeconds(1.5f);
            
            // 4. TRY TO TAKE AN ITEM
            if (targetShelf.TryTakeStock())
            {
                // Success! We got an item!
                hasItem = true;
                carriedItemVisual.SetActive(true); // Show box in hands
                ShowEmoji("😊", Color.green);
                
                yield return new WaitForSeconds(1f); // Happy pause
                
                // Go to checkout
                floatingEmoji.text = ""; // Hide emoji
                currentState = CustomerState.WalkingToCheckout;
                agent.SetDestination(targetStore.GetEntrancePosition()); // Walk to entrance/counter
            }
            else
            {
                // Failed! Shelf is empty!
                hasItem = false;
                ShowEmoji("😠", Color.red); // Angry!
                
                yield return new WaitForSeconds(1.5f); // Stare in anger
                
                // Storm out without paying
                Leave();
            }
        }

        private IEnumerator CheckoutRoutine()
        {
            currentState = CustomerState.WaitingInLine;
            
            // Look at counter
            transform.rotation = Quaternion.Euler(0, 180, 0); 
            
            // Wait for cashier (pretend for now)
            yield return new WaitForSeconds(2f);
            
            if (hasItem)
            {
                // Put box on counter (hide it from hands)
                carriedItemVisual.SetActive(false);
                
                // Drop money
                float purchaseAmount = Random.Range(50f, 200f);
                GameManager.Instance.DeductMoneyLocal(-purchaseAmount); 
                UI.HUDManager.Instance?.ShowNotification($"+Rs.{purchaseAmount:N0} Sale!", 1f);
                
                ShowEmoji("💲", Color.yellow);
            }

            yield return new WaitForSeconds(1f);
            Leave();
        }

        private void Leave()
        {
            currentState = CustomerState.Leaving;
            
            Vector3 exitPos = new Vector3(0, 0, -40); // Walk down the street
            if (NavMesh.SamplePosition(exitPos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                CustomerSpawner.Instance.RemoveCustomer(gameObject);
            }
        }

        private void ShowEmoji(string emoji, Color color)
        {
            if (floatingEmoji != null)
            {
                floatingEmoji.text = emoji;
                floatingEmoji.color = color;
            }
        }
    }
}
