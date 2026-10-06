using UnityEngine;
using System.Collections;

namespace AIBusinessTycoon.Managers
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 720f;

        [Header("Visual")]
        [SerializeField] private GameObject avatarVisual;

        [Header("Interaction")]
        [SerializeField] private PlayerInventory playerInventory;

        [Header("Supply Zone Settings")]
        [SerializeField] private float supplyPickupInterval = 0.5f;

        private IngredientPlot nearbyPlot;
        private CharacterController characterController;
        private Camera mainCamera;
        private Vector2 inputDirection;
        private bool isActive = false;

        private IngredientShelf nearbyShelf;
        private InteractableShelf nearbyCustomerShelf;
        private CraftingTable nearbyTable;
        private Dustbin nearbyDustbin;
        private StoreInteractionManager nearbyStore;
        
        // Multi-zone supply tracking
        private bool isInsideSupplyZone = false;
        private string currentSupplyItemKey = null; // Which item's zone we're in
        private Coroutine supplyPickupCoroutine = null;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            InitializeComponents();

            if (playerInventory == null)
                playerInventory = GetComponent<PlayerInventory>();
        }

        private void InitializeComponents()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (!isActive) return;

            ReadKeyboardInput();
            MoveAvatar();

            if (Input.GetKeyDown(KeyCode.E))
            {
                HandleInteraction();
            }
        }

        private void ReadKeyboardInput()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (inputDirection.magnitude < 0.1f)
            {
                inputDirection = new Vector2(h, v);
            }
        }

        public void SetJoystickInput(Vector2 direction)
        {
            inputDirection = direction;
        }

        public PlayerInventory Inventory => playerInventory;

        private void MoveAvatar()
        {
            if (inputDirection.magnitude < 0.05f) return;

            Vector3 camForward = mainCamera.transform.forward;
            Vector3 camRight = mainCamera.transform.right;

            camForward.y = 0;
            camRight.y = 0;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * inputDirection.y + camRight * inputDirection.x).normalized;

            characterController.Move(moveDir * moveSpeed * Time.deltaTime);

            if (!characterController.isGrounded)
                characterController.Move(Vector3.down * 9.81f * Time.deltaTime);

            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            inputDirection = Vector2.zero;
        }

        private void HandleInteraction()
        {
            // Supply zones are handled automatically - no E key needed
            
            if (nearbyDustbin != null)
            {
                if (nearbyDustbin.TryDiscardItem(playerInventory)) return;
            }

            if (nearbyShelf != null)
            {
                if (playerInventory.HasItem()) return;
                if (nearbyShelf.TryTakeItem()) playerInventory.PickUpItem(nearbyShelf.IngredientId);
                return;
            }

            if (nearbyPlot != null)
            {
                if (playerInventory.HasItem()) return;

                if (nearbyPlot.TryTakeOne())
                {
                    string cropName = nearbyPlot.IngredientType.ToString().ToLower();
                    playerInventory.PickUpItem(cropName);
                    Debug.Log("Harvested: " + cropName);
                }
                return;
            }

            if (nearbyTable != null)
            {
                nearbyTable.TryInteract(playerInventory);
                return;
            }

            if (nearbyCustomerShelf != null)
            {
                if (!playerInventory.HasItem())
                {
                    Debug.Log("[PlayerController] Cannot restock: not holding any items.");
                    return;
                }
                
                // Validate the held item matches this shelf's item key
                string heldItem  = playerInventory.HeldItemId;
                string shelfItem = nearbyCustomerShelf.itemKey;
                
                if (!string.Equals(heldItem, shelfItem, System.StringComparison.OrdinalIgnoreCase))
                {
                    string shelfName = nearbyCustomerShelf.itemData != null
                        ? nearbyCustomerShelf.itemData.name
                        : shelfItem;
                    Debug.LogWarning($"[PlayerController] Wrong item! Holding '{heldItem}' but shelf needs '{shelfItem}'.");
                    UI.HUDManager.Instance?.ShowNotification($"This shelf is for {shelfName}!", 1.5f);
                    return;
                }
                
                if (!nearbyCustomerShelf.CanAcceptStock())
                {
                    Debug.Log($"[PlayerController] Shelf '{shelfItem}' is already full.");
                    UI.HUDManager.Instance?.ShowNotification("Shelf is already full!", 1.5f);
                    return;
                }
                
                string itemGiven = playerInventory.DropHeldItem();
                nearbyCustomerShelf.AddStock(1);
                Debug.Log($"[PlayerController] ✅ Stocked '{shelfItem}' shelf with '{itemGiven}'.");
                return;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Supply zones are generated dynamically, so use their configured
            // component data instead of Unity tags (which must be predefined).
            SupplyZone supplyZone = other.GetComponentInParent<SupplyZone>();
            if (supplyZone != null && !string.IsNullOrEmpty(supplyZone.ItemKey))
            {
                currentSupplyItemKey = supplyZone.ItemKey;
                
                StoreInteractionManager store = supplyZone.GetComponentInParent<StoreInteractionManager>();
                if (store != null)
                {
                    nearbyStore = store;
                    isInsideSupplyZone = true;
                    StartSupplyPickup();
                    Debug.Log($"[PlayerController] 📦 Entered supply zone for: {currentSupplyItemKey}");
                }
                return;
            }

            // Regular interaction triggers
            StoreInteractionManager storeManager = other.GetComponentInParent<StoreInteractionManager>();
            if (storeManager != null)
            {
                nearbyStore = storeManager;
            }

            IngredientShelf shelf = other.GetComponentInParent<IngredientShelf>();
            if (shelf != null) nearbyShelf = shelf;

            CraftingTable table = other.GetComponentInParent<CraftingTable>();
            if (table != null) nearbyTable = table;

            InteractableShelf custShelf = other.GetComponentInParent<InteractableShelf>();
            if (custShelf != null) nearbyCustomerShelf = custShelf;

            IngredientPlot plot = other.GetComponentInParent<IngredientPlot>();
            if (plot != null) nearbyPlot = plot;

            Dustbin dustbin = other.GetComponentInParent<Dustbin>();
            if (dustbin != null) nearbyDustbin = dustbin;
        }

        private void OnTriggerExit(Collider other)
        {
            // Match the configured component data used when entering the zone.
            SupplyZone supplyZone = other.GetComponentInParent<SupplyZone>();
            if (supplyZone != null && currentSupplyItemKey == supplyZone.ItemKey)
            {
                Debug.Log($"[PlayerController] ⬅️ Exited supply zone for: {currentSupplyItemKey}");
                isInsideSupplyZone = false;
                currentSupplyItemKey = null;
                StopSupplyPickup();
                return;
            }

            StoreInteractionManager store = other.GetComponentInParent<StoreInteractionManager>();
            if (store != null && nearbyStore == store)
            {
                nearbyStore = null;
            }

            IngredientShelf shelf = other.GetComponentInParent<IngredientShelf>();
            if (shelf != null && nearbyShelf == shelf) nearbyShelf = null;

            CraftingTable table = other.GetComponentInParent<CraftingTable>();
            if (table != null && nearbyTable == table) nearbyTable = null;

            InteractableShelf custShelf = other.GetComponentInParent<InteractableShelf>();
            if (custShelf != null && nearbyCustomerShelf == custShelf) nearbyCustomerShelf = null;

            IngredientPlot plot = other.GetComponentInParent<IngredientPlot>();
            if (plot != null && nearbyPlot == plot) nearbyPlot = null;

            Dustbin dustbin = other.GetComponentInParent<Dustbin>();
            if (dustbin != null && nearbyDustbin == dustbin) nearbyDustbin = null;
        }

        private void StartSupplyPickup()
        {
            if (supplyPickupCoroutine != null)
            {
                StopCoroutine(supplyPickupCoroutine);
            }
            
            supplyPickupCoroutine = StartCoroutine(AutoPickupFromSupplyZone());
        }

        private void StopSupplyPickup()
        {
            if (supplyPickupCoroutine != null)
            {
                StopCoroutine(supplyPickupCoroutine);
                supplyPickupCoroutine = null;
            }
        }

        private IEnumerator AutoPickupFromSupplyZone()
        {
            Debug.Log($"[PlayerController] 🔄 Started auto-pickup for: {currentSupplyItemKey}");
            
            while (isInsideSupplyZone && nearbyStore != null && !string.IsNullOrEmpty(currentSupplyItemKey))
            {
                if (!playerInventory.IsFull())
                {
                    // Request specific item from this zone
                    nearbyStore.TryPlayerSupply(this, currentSupplyItemKey);
                    yield return new WaitForSeconds(supplyPickupInterval);
                }
                else
                {
                    Debug.Log($"[PlayerController] 🛑 Inventory full ({playerInventory.currentCarrying}/{playerInventory.maxCarryCapacity}). Stopping auto-pickup.");
                    break;
                }
            }
            
            Debug.Log($"[PlayerController] Auto-pickup coroutine ended for: {currentSupplyItemKey}");
            supplyPickupCoroutine = null;
        }

        public void ActivateAtPosition(Vector3 position)
        {
            InitializeComponents();

            if (characterController != null) characterController.enabled = false;
            transform.position = position;
            if (characterController != null) characterController.enabled = true;

            if (avatarVisual != null) avatarVisual.SetActive(true);
            gameObject.SetActive(true);
            isActive = true;
        }

        public void Deactivate()
        {
            isActive = false;
            
            StopSupplyPickup();
            isInsideSupplyZone = false;
            currentSupplyItemKey = null;
            
            if (avatarVisual != null) avatarVisual.SetActive(false);
            gameObject.SetActive(false);
        }

        public bool IsActive => isActive;
    }
}
