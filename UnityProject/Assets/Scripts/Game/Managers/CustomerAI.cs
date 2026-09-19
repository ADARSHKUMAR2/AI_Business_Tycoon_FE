using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services; // FIXED: Added this using statement
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
        private CheckoutCounter targetCheckout;
        private bool hasItem = false;
        
        public bool HasReachedCheckout { get; private set; } = false;

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
            GameObject cp = new GameObject("CarryPoint");
            cp.transform.SetParent(transform);
            cp.transform.localPosition = new Vector3(0, 0.6f, 0.5f); 
            carryPoint = cp.transform;

            carriedItemVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carriedItemVisual.transform.SetParent(carryPoint);
            carriedItemVisual.transform.localPosition = Vector3.zero;
            carriedItemVisual.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            carriedItemVisual.GetComponent<Renderer>().material.color = Color.red; 
            Destroy(carriedItemVisual.GetComponent<Collider>());
            carriedItemVisual.SetActive(false);

            GameObject canvasObj = new GameObject("EmojiCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, 1.8f, 0); 
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasObj.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
            canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            
            GameObject textObj = new GameObject("EmojiText");
            textObj.transform.SetParent(canvasObj.transform);
            floatingEmoji = textObj.AddComponent<TextMeshProUGUI>();
            floatingEmoji.fontSize = 50;
            floatingEmoji.alignment = TextAlignmentOptions.Center;
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(100, 100);
            textRect.localPosition = Vector3.zero;
            textRect.localRotation = Quaternion.identity;
            textRect.localScale = Vector3.one;
            
            floatingEmoji.text = ""; 
        }

        private void FindRandomStore()
        {
            StoreInteractionManager[] allStores = FindObjectsOfType<StoreInteractionManager>();
            
            if (allStores.Length == 0)
            {
                Leave();
                return;
            }

            int randomIndex = Random.Range(0, allStores.Length);
            targetStore = allStores[randomIndex];

            Vector3 entrancePos = targetStore.GetEntrancePosition();
            if (NavMesh.SamplePosition(entrancePos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                currentState = CustomerState.WalkingToStore;
                agent.SetDestination(hit.position);
                ShowEmoji("🚶", Color.white);
            }
            else
            {
                Leave();
            }
        }

        private void Update()
        {
            if (currentState == CustomerState.WalkingToStore)
            {
                if (!agent.pathPending && agent.remainingDistance <= 1.0f)
                {
                    OnArrivedAtStore();
                }
            }
            else if (currentState == CustomerState.WalkingToCheckout)
            {
                if (!agent.pathPending && agent.remainingDistance <= 0.5f)
                {
                    HasReachedCheckout = true;
                    currentState = CustomerState.WaitingInLine;
                    
                    // Face the counter
                    if (targetCheckout != null)
                    {
                        transform.rotation = Quaternion.LookRotation(targetCheckout.transform.position - transform.position);
                        transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0); 
                    }
                }
            }
        }

        private void OnArrivedAtStore()
        {
            currentState = CustomerState.Shopping;
            
            InteractableShelf[] shelves = targetStore.GetComponentsInChildren<InteractableShelf>();
            if (shelves.Length > 0)
            {
                int randomShelfIndex = Random.Range(0, shelves.Length);
                targetShelf = shelves[randomShelfIndex];
                
                StartCoroutine(ShopAtShelf());
            }
            else
            {
                Leave();
            }
        }

        private IEnumerator ShopAtShelf()
        {
            ShowEmoji("🛒", Color.white);
            
            Vector3 shelfPos = targetShelf.transform.position;
            Vector3 offset = (transform.position - shelfPos).normalized * 1.5f; 
            
            if (NavMesh.SamplePosition(shelfPos + offset, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                agent.SetDestination(shelfPos);
            }

            yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= 1.0f);
            
            yield return new WaitForSeconds(1.5f); 
            
            if (targetShelf.TryTakeStock())
            {
                hasItem = true;
                carriedItemVisual.SetActive(true); 
                ShowEmoji("😊", Color.green);
                
                yield return new WaitForSeconds(1f); 
                
                // Go to checkout
                floatingEmoji.text = ""; 
                currentState = CustomerState.WalkingToCheckout;
                
                // FIND THE CHECKOUT COUNTER IN THIS STORE
                targetCheckout = targetStore.GetComponentInChildren<CheckoutCounter>();
                
                if (targetCheckout != null)
                {
                    // Ask the counter where to stand
                    Vector3? queuePos = targetCheckout.JoinQueue(this);
                    
                    if (queuePos.HasValue)
                    {
                        agent.SetDestination(queuePos.Value);
                    }
                    else
                    {
                        // Line is full! Angry leave.
                        ShowEmoji("😠", Color.red);
                        Leave();
                    }
                }
                else
                {
                    // No checkout counter found? Just leave.
                    Leave();
                }
            }
            else
            {
                hasItem = false;
                ShowEmoji("😠", Color.red); 
                yield return new WaitForSeconds(1.5f); 
                Leave();
            }
        }

        /// <summary>
        /// Called by the CheckoutCounter when the line moves forward.
        /// </summary>
        public void MoveToNewQueuePosition(Vector3 newPos)
        {
            HasReachedCheckout = false;
            currentState = CustomerState.WalkingToCheckout;
            agent.SetDestination(newPos);
        }

        /// <summary>
        /// Called by the CheckoutCounter when the player successfully rings them up.
        /// </summary>
        public void OnPaymentComplete()
        {
            carriedItemVisual.SetActive(false);
            
            // Drop money
            float purchaseAmount = Random.Range(50f, 200f);
            GameManager.Instance.DeductMoneyLocal(-purchaseAmount); 
            UI.HUDManager.Instance?.ShowNotification($"+Rs.{purchaseAmount:N0} Sale!", 1f);

            // Phase 3: Random chance to drop trash
            if (UnityEngine.Random.value < 0.3f) // 30% chance
            {
                DropTrash();
            }
            
            ShowEmoji("💲", Color.yellow);
            
            StartCoroutine(LeaveAfterDelay());
        }

        private void DropTrash()
        {
            var gm = Managers.GameManager.Instance;
            if (gm?.CurrentPlayer == null || targetStore?.BusinessData == null) return;

            // The trash drops at the customer's current world position
            float worldX = transform.position.x;
            float worldZ = transform.position.z; // Backend stores as position_y

            var request = new SpawnTrashRequest(worldX, worldZ);

            TycoonAPIService.Instance.SpawnTrash(
                gm.CurrentPlayer.player_id,
                targetStore.BusinessData.business_id,
                request,
                (updatedBusiness) =>
                {
                    Debug.Log($"[CustomerAI] Dropped trash. Store rating: {updatedBusiness.store_rating}");

                    // Update local business data
                    targetStore.BusinessData.store_rating = updatedBusiness.store_rating;
                    targetStore.BusinessData.trash_items  = updatedBusiness.trash_items;

                    // Notify any CleanerAI in this store about the new trash
                    // Get the newly created TrashItem (last in the list)
                    var newTrash = updatedBusiness.trash_items;
                    if (newTrash != null && newTrash.Count > 0)
                    {
                        CleanerAI cleaner = targetStore.GetComponentInChildren<CleanerAI>();
                        cleaner?.NotifyNewTrash(newTrash[newTrash.Count - 1]);
                    }
                },
                (err) => Debug.LogWarning($"[CustomerAI] Failed to spawn trash on backend: {err}")
            );
        }

        private IEnumerator LeaveAfterDelay()
        {
            yield return new WaitForSeconds(1f);
            Leave();
        }

        private void Leave()
        {
            currentState = CustomerState.Leaving;
            
            Vector3 exitPos = new Vector3(0, 0, -40); 
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
