using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;
using TMPro; 
using UnityEngine.UI;

namespace AIBusinessTycoon.Managers
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class CustomerAI : MonoBehaviour
    {
        private enum CustomerState { Initializing, WalkingToStore, Shopping, WaitingAtShelf, WalkingToCheckout, WaitingAtCounter, Leaving }
        
        private CustomerState currentState = CustomerState.Initializing;
        private NavMeshAgent agent;
        private StoreInteractionManager targetStore;
        
        [Header("Visuals")]
        private Transform carryPoint;
        private TextMeshProUGUI floatingEmoji;
        private Image statusIcon;
        private Dictionary<string, Sprite> statusSprites = new Dictionary<string, Sprite>();
        
        // ── Local Object Pool for Boxes ──
        private List<GameObject> boxPool = new List<GameObject>();
        private int activeBoxCount = 0;
        
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
            
            // Reset pooled boxes (hide them all)
            activeBoxCount = 0;
            foreach (var box in boxPool)
            {
                if (box != null) box.SetActive(false);
            }

            ShowStatusIcon(string.Empty, Color.white);
            
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
            // 1. Setup Carry Point
            carryPoint = transform.Find("CarryPoint");
            if (carryPoint == null)
            {
                Debug.LogWarning($"[CustomerAI] '{gameObject.name}' is missing the 'CarryPoint' object. Please run the prefab update tool.");
                return;
            }

            // 2. Initialize Local Object Pool from prefab children
            boxPool.Clear();
            foreach (Transform child in carryPoint)
            {
                if (child.name.StartsWith("PooledBox"))
                {
                    child.gameObject.SetActive(false);
                    boxPool.Add(child.gameObject);
                }
            }

            if (boxPool.Count == 0)
            {
                Debug.LogWarning($"[CustomerAI] '{gameObject.name}' CarryPoint has no pooled boxes. Please run the prefab update tool.");
            }

            // 3. Setup status icon canvas.
            Transform existingCanvas = transform.Find("EmojiCanvas");
            if (existingCanvas == null)
            {
                Debug.LogWarning($"[CustomerAI] '{gameObject.name}' is missing the 'EmojiCanvas' object. Please run the prefab update tool.");
                return;
            }

            // Disable the old TMP text component if it still exists.
            floatingEmoji = existingCanvas.GetComponentInChildren<TextMeshProUGUI>();
            if (floatingEmoji != null) floatingEmoji.gameObject.SetActive(false);

            statusIcon = existingCanvas.GetComponentsInChildren<Image>(true).FirstOrDefault(img => img.name == "StatusIcon");
            if (statusIcon == null)
            {
                Debug.LogWarning($"[CustomerAI] '{gameObject.name}' is missing the 'StatusIcon' Image under 'EmojiCanvas'. Please run the prefab update tool.");
                return;
            }

            statusSprites.Clear();
            Sprite[] loadedSprites = Resources.LoadAll<Sprite>("Sprite Assets/CustomerStatusIcons");
            foreach (Sprite sprite in loadedSprites)
            {
                statusSprites[sprite.name] = sprite;
            }
        }

        private void FindRandomStore()
        {
            var stores = FindObjectsOfType<StoreInteractionManager>();
            if (stores.Length > 0)
            {
                targetStore = stores[Random.Range(0, stores.Length)];
                
                if (targetStore.BusinessData != null && targetStore.BusinessData.inventory != null)
                {
                    // ONLY grab items from the backend that are marked as "is_sellable = true"
                    var menuItems = targetStore.BusinessData.inventory
                        .Where(item => item.Value.is_sellable)
                        .ToList();

                    // If the store actually has sellable menu items available
                    if (menuItems.Count > 0)
                    {
                        int itemsCount = Random.Range(1, 4); 
                        for(int i = 0; i < itemsCount; i++) 
                        {
                            string randomKey = menuItems[Random.Range(0, menuItems.Count)].Key;
                            itemsToBuy.Add(randomKey);
                        }
                    }
                }

                // If they couldn't find anything sellable, they leave
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

            string currentItemToBuy = itemsToBuy[0];
            var shelves = targetStore.GetComponentsInChildren<InteractableShelf>();
            
            targetShelf = shelves.FirstOrDefault(s => s.itemKey == currentItemToBuy);

            if (targetShelf != null)
            {
                currentState = CustomerState.Shopping;
                Vector3 destination = targetShelf.transform.position + new Vector3(0, 0, -1.0f);
                agent.SetDestination(destination);
            }
            else
            {
                itemsWaiting.Add(currentItemToBuy);
                itemsToBuy.RemoveAt(0);
                
                FindShelf(); 
            }
        }

        private IEnumerator GrabItem()
        {
            if (targetStore != null && targetStore.BusinessData != null && itemsToBuy.Count > 0)
            {
                string currentItem = itemsToBuy[0];
                var inventory = targetStore.BusinessData.inventory;

                if (inventory.ContainsKey(currentItem) && inventory[currentItem].stock > 0)
                {
                    ShowEmoji("🛒", Color.white);
                    yield return new WaitForSeconds(1.0f);

                    if (targetShelf == null || !targetShelf.TryTakeStock())
                    {
                        ShowEmoji("❌", Color.red);
                        yield return new WaitForSeconds(1.0f);
                        BeginWaitingAtShelf(currentItem);
                        yield break;
                    }

                    // Keep the store inventory synchronized with the shelf's local stock.
                    // UpdateShelfVisuals reads from BusinessData.inventory, so the backend
                    // mirror must also be decremented when the customer takes the item.
                    var updatedItem = inventory[currentItem];
                    updatedItem.stock = Mathf.Max(0, updatedItem.stock - 1);
                    inventory[currentItem] = updatedItem;
                    
                    itemsInCart.Add(currentItem);
                    UpdateShelfVisuals(currentItem);
                    
                    // Show next pooled box instead of instantiating
                    if (activeBoxCount < boxPool.Count)
                    {
                        boxPool[activeBoxCount].SetActive(true);
                        activeBoxCount++;
                    }
                }
                else
                {
                    ShowEmoji("❌", Color.red);
                    yield return new WaitForSeconds(1.0f);
                    BeginWaitingAtShelf(currentItem);
                    yield break;
                }

                itemsToBuy.RemoveAt(0);
            }

            if (itemsToBuy.Count > 0)
            {
                hasItem = false; 
                FindShelf();     
            }
            else
            {
                GoToCheckout();  
            }
        }

        private void BeginWaitingAtShelf(string itemKey)
        {
            if (targetShelf == null)
            {
                itemsWaiting.Add(itemKey);
                FinishShoppingOrGoToCheckout();
                return;
            }

            if (itemsToBuy.Count > 0 && itemsToBuy[0] == itemKey)
            {
                itemsToBuy.RemoveAt(0);
            }

            itemsWaiting.Remove(itemKey);
            currentState = CustomerState.WaitingAtShelf;
            hasItem = false;
            waitTimer = 15f;

            if (agent.isOnNavMesh)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            ShowEmoji("⏳", Color.yellow);
            if (waitCoroutine == null)
            {
                waitCoroutine = StartCoroutine(WaitForShelfStockRoutine(itemKey));
            }
        }

        private IEnumerator WaitForShelfStockRoutine(string itemKey)
        {
            while (waitTimer > 0f)
            {
                if (targetShelf != null && targetShelf.itemKey == itemKey && targetShelf.currentStock > 0)
                {
                    if (targetShelf.TryTakeStock())
                    {
                        if (targetStore?.BusinessData?.inventory != null &&
                            targetStore.BusinessData.inventory.TryGetValue(itemKey, out var updatedItem))
                        {
                            updatedItem.stock = Mathf.Max(0, updatedItem.stock - 1);
                            targetStore.BusinessData.inventory[itemKey] = updatedItem;
                        }

                        itemsInCart.Add(itemKey);
                        UpdateShelfVisuals(itemKey);
                        ShowEmoji("🛒", Color.white);

                        if (activeBoxCount < boxPool.Count)
                        {
                            boxPool[activeBoxCount].SetActive(true);
                            activeBoxCount++;
                        }

                        waitCoroutine = null;
                        FinishShoppingOrGoToCheckout();
                        yield break;
                    }
                }

                yield return new WaitForSeconds(1f);
                waitTimer -= 1f;
            }

            waitCoroutine = null;
            ShowEmoji("😠", Color.red);
            Leave();
        }

        private void FinishShoppingOrGoToCheckout()
        {
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }

            if (itemsToBuy.Count > 0)
            {
                hasItem = false;
                FindShelf();
            }
            else if (itemsWaiting.Count > 0)
            {
                currentState = CustomerState.WaitingAtCounter;
                if (waitCoroutine == null)
                {
                    waitCoroutine = StartCoroutine(WaitTimerRoutine());
                }
            }
            else
            {
                GoToCheckout();
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

            // Hide boxes to simulate placing them on the counter
            activeBoxCount = 0;
            foreach (var box in boxPool)
            {
                if (box != null) box.SetActive(false);
            }

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
                    break; 
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
            // Ensure boxes are hidden
            activeBoxCount = 0;
            foreach (var box in boxPool)
            {
                if (box != null) box.SetActive(false);
            }
            
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

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
            }

            // Use the new method instead of the spawner's transform
            Vector3 exitPos = CustomerSpawner.Instance.GetExitPosition();

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
            string spriteName = emoji switch
            {
                "⏳" => "waiting",
                "😠" => "error",
                "🛒" => "shopping",
                "💲" => "success",
                "❌" => "error",
                _ => string.Empty
            };

            ShowStatusIcon(spriteName, color);
        }

        private void ShowStatusIcon(string spriteName, Color color)
        {
            if (statusIcon == null)
                return;

            statusIcon.color = color;
            statusIcon.sprite = string.IsNullOrEmpty(spriteName) || !statusSprites.TryGetValue(spriteName, out Sprite sprite)
                ? null
                : sprite;
            statusIcon.enabled = statusIcon.sprite != null;
        }
    }
}
