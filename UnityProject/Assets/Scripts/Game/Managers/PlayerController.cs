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
        [SerializeField] private float supplyPickupInterval = 0.5f; // Time between each pickup in supply zone

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
        
        // Supply zone tracking
        private bool isInsideSupplyZone = false;
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
            // Skip supply zone here - it's handled automatically in OnTriggerEnter/Exit
            
            // Original interaction flow retained
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
                if (playerInventory.HasItem() && nearbyCustomerShelf.CanAcceptStock())
                {
                    string itemGiven = playerInventory.DropHeldItem();
                    nearbyCustomerShelf.AddStock(1);
                    Debug.Log($"Stocked customer shelf with {itemGiven}!");
                }
                return;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            StoreInteractionManager store = other.GetComponentInParent<StoreInteractionManager>();
            if (store != null)
            {
                nearbyStore = store;
                
                // Check if we specifically entered the supply zone
                if (store.SupplyZone != null && 
                    (other.gameObject == store.SupplyZone || other.transform.IsChildOf(store.SupplyZone.transform)))
                {
                    isInsideSupplyZone = true;
                    StartSupplyPickup();
                    Debug.Log($"[PlayerController] Entered supply zone for store {store.BusinessData?.business_id}");
                }
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
            StoreInteractionManager store = other.GetComponentInParent<StoreInteractionManager>();
            if (store != null && nearbyStore == store)
            {
                // Check if we exited the supply zone specifically
                if (store.SupplyZone != null && 
                    (other.gameObject == store.SupplyZone || other.transform.IsChildOf(store.SupplyZone.transform)))
                {
                    isInsideSupplyZone = false;
                    StopSupplyPickup();
                    Debug.Log($"[PlayerController] Exited supply zone for store {store.BusinessData?.business_id}");
                }
                
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
            // Stop any existing pickup coroutine
            if (supplyPickupCoroutine != null)
            {
                StopCoroutine(supplyPickupCoroutine);
            }
            
            // Start automatic pickup
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
            Debug.Log("[PlayerController] Started auto-pickup from supply zone");
            
            while (isInsideSupplyZone && nearbyStore != null)
            {
                // Keep picking up until inventory is full
                if (!playerInventory.IsFull())
                {
                    nearbyStore.TryPlayerSupply(this);
                    yield return new WaitForSeconds(supplyPickupInterval);
                }
                else
                {
                    Debug.Log($"[PlayerController] Inventory full ({playerInventory.currentCarrying}/{playerInventory.maxCarryCapacity}). Stopping auto-pickup.");
                    break;
                }
            }
            
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
            
            // Stop any ongoing supply pickup
            StopSupplyPickup();
            isInsideSupplyZone = false;
            
            if (avatarVisual != null) avatarVisual.SetActive(false);
            gameObject.SetActive(false);
        }

        public bool IsActive => isActive;
    }
}
