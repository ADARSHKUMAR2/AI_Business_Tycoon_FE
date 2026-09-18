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

            targetStore = allStores[Random.Range(0, allStores.Length)];
            currentState = CustomerState.WalkingToStore;
            agent.SetDestination(targetStore.GetEntrancePosition());
        }

        private void Update()
        {
            if (currentState == CustomerState.Initializing || !agent.isOnNavMesh) return;

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
                    if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                    {
                        // We arrived at our spot in line!
                        HasReachedCheckout = true;
                        currentState = CustomerState.WaitingInLine;
                        
                        // Look forward toward the counter
                        if (targetCheckout != null)
                        {
                            transform.LookAt(new Vector3(targetCheckout.transform.position.x, transform.position.y, targetCheckout.transform.position.z));
                        }
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
            
            InteractableShelf[] shelves = targetStore.GetComponentsInChildren<InteractableShelf>();
            
            if (shelves.Length == 0)
            {
                ShowEmoji("😠", Color.red);
                Leave();
                yield break;
            }

            targetShelf = shelves[Random.Range(0, shelves.Length)];
            
            Vector3 targetPos = targetShelf.transform.position + new Vector3(1.5f, 0, 0); 
            if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            
            yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f);
            
            transform.LookAt(new Vector3(targetShelf.transform.position.x, transform.position.y, targetShelf.transform.position.z));
            
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
            
            ShowEmoji("💲", Color.yellow);
            
            StartCoroutine(LeaveAfterDelay());
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
