using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;
using TMPro; 

namespace AIBusinessTycoon.Managers
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class CustomerAI : MonoBehaviour
    {
        private enum CustomerState { Initializing, WalkingToStore, Shopping, WalkingToCheckout, WaitingAtCounter, Leaving }
        
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

        // Local Shopping Logic
        private List<string> itemsToBuy = new List<string>();
        private List<string> itemsInCart = new List<string>(); 
        private List<string> itemsWaiting = new List<string>();
        private List<string> itemsPurchased = new List<string>();
        private float totalSpent = 0f;
        private float waitTimer = 15f; 
        private Coroutine waitCoroutine;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            SetupVisuals();
        }

        private void OnEnable()
        {
            currentState = CustomerState.Initializing;
            hasItem = false;
            HasReachedCheckout = false;
            targetStore = null;
            targetShelf = null;
            targetCheckout = null;
            
            itemsToBuy.Clear();
            itemsInCart.Clear(); 
            itemsWaiting.Clear();
            itemsPurchased.Clear();
            totalSpent = 0f;
            waitTimer = 15f;
            
            if (waitCoroutine != null)
            {
                StopCoroutine(waitCoroutine);
                waitCoroutine = null;
            }
            
            if (carriedItemVisual != null) carriedItemVisual.SetActive(false);
            ShowEmoji("", Color.white);
            
            if (agent != null && agent.isOnNavMesh) 
            {
                agent.ResetPath();
                agent.isStopped = false;
            }

            StartCoroutine(BeginShoppingRoutine());
        }

        private IEnumerator BeginShoppingRoutine()
        {
            yield return new WaitUntil(() => agent != null && agent.isOnNavMesh);
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
            FindRandomStore();
        }

        private void SetupVisuals()
        {
            Transform existingCp = transform.Find("CarryPoint");
            if (existingCp != null)
            {
                carryPoint = existingCp;
                
                Transform visualTransform = carryPoint.Find("CarriedItemBox");
                if (visualTransform != null) {
                    carriedItemVisual = visualTransform.gameObject;
                } else {
                    carriedItemVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    carriedItemVisual.name = "CarriedItemBox";
                    carriedItemVisual.transform.SetParent(carryPoint);
                    carriedItemVisual.transform.localPosition = Vector3.zero;
                    carriedItemVisual.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                    carriedItemVisual.GetComponent<Renderer>().material.color = Color.red; 
                    Destroy(carriedItemVisual.GetComponent<Collider>());
                }

                floatingEmoji = transform.GetComponentInChildren<TextMeshProUGUI>();
                return;
            }

            GameObject cp = new GameObject("CarryPoint");
            cp.transform.SetParent(transform);
            cp.transform.localPosition = new Vector3(0, 0.6f, 0.5f); 
            carryPoint = cp.transform;

            carriedItemVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            carriedItemVisual.name = "CarriedItemBox"; 
            carriedItemVisual.transform.SetParent(carryPoint);
            carriedItemVisual.transform.localPosition = Vector3.zero;
            carriedItemVisual.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            carriedItemVisual.GetComponent<Renderer>().material.color = Color.red; 
            Destroy(carriedItemVisual.GetComponent<Collider>());
            carriedItemVisual.SetActive(false);

            GameObject canvasObj = new GameObject("EmojiCanvas");
            canvasObj.transform.SetParent(transform);
            canvasObj.transform.localPosition = new Vector3(0, 2.2f, 0); 
            
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2f, 2f); 
            
            GameObject textObj = new GameObject("EmojiText");
            textObj.transform.SetParent(canvasObj.transform);
            
            floatingEmoji = textObj.AddComponent<TextMeshProUGUI>();
            floatingEmoji.alignment = TextAlignmentOptions.Center;
            floatingEmoji.fontSize = 5; 
            floatingEmoji.text = "";
            
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.localPosition = Vector3.zero;
            textRect.sizeDelta = new Vector2(2f, 2f); 
            textRect.localScale = Vector3.one; 
        }

        private void FindRandomStore()
        {
            var stores = FindObjectsOfType<StoreInteractionManager>();
            if (stores.Length > 0)
            {
                targetStore = stores[Random.Range(0, stores.Length)];
                
                if (targetStore.BusinessData != null && targetStore.BusinessData.inventory != null)
                {
                    var activeItems = targetStore.BusinessData.inventory.ToList();
                    
                    if (activeItems.Count > 0)
                    {
                        int itemsCount = Random.Range(1, 4); 
                        for(int i = 0; i < itemsCount; i++) 
                        {
                            string randomKey = activeItems[Random.Range(0, activeItems.Count)].Key;
                            itemsToBuy.Add(randomKey);
                        }
                    }
                }

                if (itemsToBuy.Count == 0)
                {
                    Leave();
                    return;
                }

                currentState = CustomerState.WalkingToStore;
                Vector3 storeEntrance = targetStore.transform.position + new Vector3(0, 0, -2);
                agent.SetDestination(storeEntrance);
            }
            else
            {
                Leave();
            }
        }

        private void Update()
        {
            if (floatingEmoji != null)
            {
                floatingEmoji.transform.rotation = Quaternion.LookRotation(floatingEmoji.transform.position - Camera.main.transform.position);
            }

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            {
                if (agent.velocity.sqrMagnitude < 0.1f)
                {
                    if (currentState == CustomerState.WalkingToStore)
                    {
                        FindShelf();
                    }
                    else if (currentState == CustomerState.Shopping && !hasItem)
                    {
                        hasItem = true; 
                        StartCoroutine(GrabItem());
                    }
                    else if (currentState == CustomerState.WalkingToCheckout && !HasReachedCheckout)
                    {
                        HasReachedCheckout = true;
                    }
                    else if (currentState == CustomerState.Leaving)
                    {
                        CustomerSpawner.Instance.ReturnCustomerToPool(gameObject);
                    }
                }
            }
        }

        private void FindShelf()
        {
            if (targetStore == null || itemsToBuy.Count == 0) 
            { 
                GoToCheckout(); 
                return; 
            }

            // Look for the SPECIFIC shelf that matches the FIRST item on their list
            string currentItemToBuy = itemsToBuy[0];
            var shelves = targetStore.GetComponentsInChildren<InteractableShelf>();
            
            // Find the shelf that holds this specific item
            targetShelf = shelves.FirstOrDefault(s => s.itemKey == currentItemToBuy);

            if (targetShelf != null)
            {
                currentState = CustomerState.Shopping;
                Vector3 destination = targetShelf.transform.position + new Vector3(0, 0, -1.0f);
                agent.SetDestination(destination);
            }
            else
            {
                // If the store doesn't have a shelf for this item physically placed yet, 
                // they will ask the cashier for it instead.
                itemsWaiting.Add(currentItemToBuy);
                itemsToBuy.RemoveAt(0);
                
                // Immediately search for the next shelf on their list
                FindShelf(); 
            }
        }

        private IEnumerator GrabItem()
        {
            if (targetStore != null && targetStore.BusinessData != null && itemsToBuy.Count > 0)
            {
                string currentItem = itemsToBuy[0];
                var inventory = targetStore.BusinessData.inventory;

                // Check if the item is in stock physically
                if (inventory.ContainsKey(currentItem) && inventory[currentItem].stock > 0)
                {
                    ShowEmoji("🛒", Color.white);
                    yield return new WaitForSeconds(1.0f);

                    var item = inventory[currentItem];
                    item.stock = Mathf.Max(0, item.stock - 1);
                    inventory[currentItem] = item; // Update dictionary reference
                    
                    itemsInCart.Add(currentItem);
                    UpdateShelfVisuals(currentItem);
                    
                    if (carriedItemVisual != null) carriedItemVisual.SetActive(true);
                }
                else
                {
                    // Out of stock on the shelf - show a red X emoji
                    ShowEmoji("❌", Color.red);
                    yield return new WaitForSeconds(1.0f);
                    
                    // They will ask the cashier for it instead
                    itemsWaiting.Add(currentItem);
                }

                // Remove the item from the remaining shopping list
                itemsToBuy.RemoveAt(0);
            }

            // Do they have more items on their list?
            if (itemsToBuy.Count > 0)
            {
                hasItem = false; // Reset the interaction trigger
                FindShelf();     // Walk to the next shelf!
            }
            else
            {
                GoToCheckout();  // Shopping complete, go pay
            }
        }

        private void GoToCheckout()
        {
            if (targetStore == null) { Leave(); return; }

            targetCheckout = targetStore.GetComponentInChildren<CheckoutCounter>();
            if (targetCheckout != null)
            {
                Vector3? queuePos = targetCheckout.JoinQueue(this);

                if (queuePos.HasValue)
                {
                    currentState = CustomerState.WalkingToCheckout;
                    agent.SetDestination(queuePos.Value);
                }
                else
                {
                    // If queue is full, they leave angry and "steal" the items in their cart
                    ShowEmoji("😠", Color.red);
                    Leave(); 
                }
            }
            else
            {
                Leave();
            }
        }

        public void MoveToNewQueuePosition(Vector3 newPos)
        {
            HasReachedCheckout = false;
            currentState = CustomerState.WalkingToCheckout;
            agent.SetDestination(newPos);
        }
        
        public void OnPaymentComplete()
        {
            ProcessLocalPurchase();
        }

        private void ProcessLocalPurchase()
        {
            if (targetStore == null || targetStore.BusinessData == null) 
            { 
                CompleteTransactionAndLeave(); 
                return; 
            }

            var inventory = targetStore.BusinessData.inventory;
            var priceMultiplier = targetStore.BusinessData.price_multiplier;

            // 1. Pay for items already grabbed from shelves
            foreach (var itemKey in itemsInCart)
            {
                if (inventory.ContainsKey(itemKey))
                {
                    float price = inventory[itemKey].price * priceMultiplier;
                    totalSpent += price;
                    itemsPurchased.Add(itemKey);
                }
            }
            itemsInCart.Clear(); 

            // 2. Process out-of-stock items they requested from cashier
            List<string> stillWaiting = new List<string>();

            foreach (var itemKey in itemsWaiting)
            {
                if (inventory.ContainsKey(itemKey) && inventory[itemKey].stock > 0)
                {
                    var item = inventory[itemKey];
                    float price = item.price * priceMultiplier;
                    totalSpent += price;
                    itemsPurchased.Add(itemKey);
                    
                    item.stock = Mathf.Max(0, item.stock - 1);
                    inventory[itemKey] = item;
                    
                    UpdateShelfVisuals(itemKey);
                }
                else
                {
                    stillWaiting.Add(itemKey);
                }
            }

            itemsWaiting = stillWaiting;

            if (itemsWaiting.Count > 0)
            {
                currentState = CustomerState.WaitingAtCounter;
                if (waitCoroutine == null)
                {
                    waitCoroutine = StartCoroutine(WaitTimerRoutine());
                }
            }
            else
            {
                if (waitCoroutine != null)
                {
                    StopCoroutine(waitCoroutine);
                    waitCoroutine = null;
                }
                CompleteTransactionAndLeave();
            }
        }
        
        private void UpdateShelfVisuals(string itemKey)
        {
            if (targetStore == null) return;
            
            var shelves = targetStore.GetComponentsInChildren<InteractableShelf>();
            foreach (var shelf in shelves)
            {
                if (shelf.itemKey == itemKey)
                {
                    if (targetStore.BusinessData.inventory.ContainsKey(itemKey))
                    {
                        int currentStock = targetStore.BusinessData.inventory[itemKey].stock;
                        shelf.currentStock = Mathf.Clamp(currentStock, 0, shelf.maxCapacity);
                        shelf.UpdateVisuals(); 
                    }
                    break; // Fixed: only updates the EXACT shelf matched
                }
            }
        }

        private IEnumerator WaitTimerRoutine()
        {
            ShowEmoji("⏳", Color.yellow);
            
            while (waitTimer > 0)
            {
                yield return new WaitForSeconds(1f);
                waitTimer -= 1f;
                
                if (targetStore?.BusinessData?.inventory != null)
                {
                    bool allFulfilled = true;
                    foreach (var itemKey in itemsWaiting)
                    {
                        if (!targetStore.BusinessData.inventory.ContainsKey(itemKey) || 
                            targetStore.BusinessData.inventory[itemKey].stock <= 0)
                        {
                            allFulfilled = false;
                            break;
                        }
                    }
                    
                    if (allFulfilled)
                    {
                        waitCoroutine = null;
                        ProcessLocalPurchase();
                        yield break;
                    }
                }
            }

            waitCoroutine = null;
            ShowEmoji("😠", Color.red);
            CompleteTransactionAndLeave();
        }

        private void CompleteTransactionAndLeave()
        {
            if (carriedItemVisual != null) carriedItemVisual.SetActive(false);
            
            if (totalSpent > 0)
            {
                GameManager.Instance.DeductMoneyLocal(-totalSpent); 
                UI.HUDManager.Instance?.ShowNotification($"+Rs.{totalSpent:N0} Sale!", 1f);
                ShowEmoji("💲", Color.green);

                var syncManager = targetStore.GetComponent<TransactionSyncManager>();
                if (syncManager != null)
                {
                    syncManager.RecordSale(itemsPurchased, totalSpent);
                }
            }
            else
            {
                ShowEmoji("❌", Color.red); 
            }

            if (Random.value < 0.3f && totalSpent > 0) 
            {
                StartCoroutine(DelayedDropTrash());
            }
            
            StartCoroutine(LeaveAfterDelay());
        }

        private IEnumerator DelayedDropTrash()
        {
            yield return new WaitForSeconds(1.5f);
            DropTrash();
        }

        private void DropTrash()
        {
            var gm = Managers.GameManager.Instance;
            if (gm?.CurrentPlayer == null || targetStore?.BusinessData == null) return;

            float worldX = transform.position.x;
            float worldZ = transform.position.z; 

            var request = new SpawnTrashRequest(worldX, worldZ);

            TycoonAPIService.Instance.SpawnTrash(
                gm.CurrentPlayer.player_id,
                targetStore.BusinessData.business_id,
                request,
                (updatedBusiness) =>
                {
                    targetStore.BusinessData.store_rating = updatedBusiness.store_rating;
                    targetStore.BusinessData.trash_items  = updatedBusiness.trash_items;

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
            
            Vector3 exitPos = CustomerSpawner.Instance.transform.position; 
            
            if (NavMesh.SamplePosition(exitPos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                CustomerSpawner.Instance.ReturnCustomerToPool(gameObject);
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
