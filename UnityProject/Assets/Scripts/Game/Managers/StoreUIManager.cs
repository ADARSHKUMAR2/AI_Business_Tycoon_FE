using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;
using AIBusinessTycoon.Managers; // FIXED: Added this to recognize CleanerAI
using System.Collections.Generic;

namespace AIBusinessTycoon.UI
{
    /// <summary>
    /// UI shown when the player is INSIDE a store.
    /// Phase 2: Added Hire Staff panel with Cashier and Restocker hiring buttons.
    /// Phase 3: Added Cleaner hiring button and store rating.
    /// </summary>
    public class StoreUIManager : MonoBehaviour
    {
        public static StoreUIManager Instance { get; private set; }

        // ── Panels ──────────────────────────────────────────────────────────
        [Header("Panels")]
        [SerializeField] private GameObject storeUIPanel;
        [SerializeField] private GameObject hireMenuPanel;   // Phase 2: The hire popup

        // ── Store Info ───────────────────────────────────────────────────────
        [Header("Store Info")]
        [SerializeField] private TextMeshProUGUI storeNameText;
        [SerializeField] private TextMeshProUGUI storeTypeText;

        // ── Joystick ─────────────────────────────────────────────────────────
        [Header("Joystick")]
        [SerializeField] private GameObject joystickPanel;
        [SerializeField] private JoystickController joystick;

        // ── Main Buttons ─────────────────────────────────────────────────────
        [Header("Buttons")]
        [SerializeField] private Button exitButton;
        [SerializeField] private Button hireStaffButton;     // Phase 2: Opens hire menu

        // ── Hire Menu Buttons ─────────────────────────────────────────────────
        [Header("Hire Menu Buttons")]
        [SerializeField] private Button hireCashierButton;   // Rs. 1,000
        [SerializeField] private Button hireRestockerButton; // Rs. 1,500
        [SerializeField] private Button closeHireMenuButton;
        [SerializeField] private Button             hireCleanerButton;
        [SerializeField] private TextMeshProUGUI    cleanerStatusText;
        [SerializeField] private TextMeshProUGUI    storeRatingText;    // Phase 3: shows ⭐ x.x/5.0
        [SerializeField] private GameObject         cleanerPrefab;

        // ── Hire Menu Labels (to show affordability) ──────────────────────────
        [Header("Hire Menu Labels")]
        [SerializeField] private TextMeshProUGUI cashierStatusText;
        [SerializeField] private TextMeshProUGUI restockerStatusText;

        // ── Employee Prefabs ───────────────────────────────────────────────────
        [Header("Employee Prefabs (Phase 2)")]
        [SerializeField] private GameObject cashierPrefab;
        [SerializeField] private GameObject restockerPrefab;

        // ── Costs ─────────────────────────────────────────────────────────────
        private const float CashierCost   = 1000f;
        private const float RestockerCost = 1500f;
        private const float CleanerCost = 800f;
        private bool cleanerHired = false;

        // ── Internal State ─────────────────────────────────────────────────────
        private BusinessData currentBusiness;
        private Managers.StoreInteractionManager currentStore;

        // Track which employee types have been hired this session
        private bool cashierHired    = false;
        private bool restockerHired  = false;

        // ──────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            // Wire up button listeners
            if (exitButton          != null) exitButton.onClick.AddListener(OnExitClicked);
            if (hireStaffButton     != null) hireStaffButton.onClick.AddListener(OnHireStaffClicked);
            if (closeHireMenuButton != null) closeHireMenuButton.onClick.AddListener(CloseHireMenu);
            if (hireCashierButton   != null) hireCashierButton.onClick.AddListener(OnHireCashierClicked);
            if (hireRestockerButton != null) hireRestockerButton.onClick.AddListener(OnHireRestockerClicked);
            if (hireCleanerButton   != null) hireCleanerButton.onClick.AddListener(OnHireCleanerClicked);

            storeUIPanel?.SetActive(false);
            hireMenuPanel?.SetActive(false);
        }

        // ──────────────────────────────────────────────────────────────────────
        #region Public API (Opened by GameManager)

        public void OpenStoreUI(Managers.StoreInteractionManager store)
        {
            currentStore    = store;
            currentBusiness = store.BusinessData;

            if (storeNameText != null) storeNameText.text = currentBusiness.name;
            if (storeTypeText != null) storeTypeText.text = currentBusiness.business_type.ToUpper();

            // Check existing employees from backend to set UI state
            cashierHired   = false;
            restockerHired = false;
            cleanerHired   = false;

            if (currentBusiness.employees != null)
            {
                foreach (var emp in currentBusiness.employees)
                {
                    if (emp.role == "cashier")   cashierHired   = true;
                    if (emp.role == "restocker") restockerHired = true;
                    if (emp.role == "cleaner")   cleanerHired   = true;
                }
            }

            storeUIPanel?.SetActive(true);
            UI.HUDManager.Instance?.ShowHUD(false); // Hide main HUD
            EnableJoystick(true);

            Debug.Log($"[StoreUIManager] Opened UI for {currentBusiness.name}");
        }

        public void CloseStoreUI()
        {
            storeUIPanel?.SetActive(false);
            hireMenuPanel?.SetActive(false);
            UI.HUDManager.Instance?.ShowHUD(true); // Restore main HUD
            EnableJoystick(false);
            
            currentBusiness = null;
            currentStore    = null;
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Input / UI Handlers

        private void EnableJoystick(bool enable)
        {
            if (joystickPanel != null) joystickPanel.SetActive(enable);

        }

        private void OnExitClicked()
        {
            Managers.GameManager.Instance.ExitStore();
        }

        private void OnHireStaffClicked()
        {
            EnableJoystick(false); // Disable movement while menu is open
            RefreshHireMenuLabels();
            hireMenuPanel?.SetActive(true);
        }

        private void CloseHireMenu()
        {
            hireMenuPanel?.SetActive(false);
            EnableJoystick(true);
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Hiring Logic

        private void OnHireCashierClicked()
        {
            var gm = Managers.GameManager.Instance;
            if (gm?.CurrentPlayer == null || currentBusiness == null) return;

            if (gm.CurrentPlayer.money < CashierCost)
            {
                UI.HUDManager.Instance?.ShowNotification("❌ Not enough money for Cashier!", 2f);
                return;
            }

            cashierHired = true;
            gm.DeductMoneyLocal(CashierCost);
            CloseHireMenu();
            RefreshHireMenuLabels();

            string cashierName = "Cashier_" + System.Guid.NewGuid().ToString()[..4];
            TycoonAPIService.Instance.HireEmployee(
                gm.CurrentPlayer.player_id,
                currentBusiness.business_id,
                new EmployeeHireRequest(cashierName, "cashier"),
                (emp) => 
                {
                    Debug.Log($"[StoreUIManager] Cashier hired and saved to backend: {emp.name}");
                    // 1. Add to local state
                    if (currentBusiness.employees == null) currentBusiness.employees = new System.Collections.Generic.List<Employee>();
                    currentBusiness.employees.Add(emp);
                    
                    // 2. NOW spawn the AI, because the data exists!
                    SpawnEmployeeAI("cashier");
                },
                (err) => 
                {
                    Debug.LogWarning($"[StoreUIManager] Backend hire failed: {err}");
                    // Refund if failed
                    cashierHired = false;
                    gm.DeductMoneyLocal(-CashierCost);
                }
            );
        }

        private void OnHireRestockerClicked()
        {
            var gm = Managers.GameManager.Instance;
            if (gm?.CurrentPlayer == null || currentBusiness == null) return;

            if (gm.CurrentPlayer.money < RestockerCost)
            {
                UI.HUDManager.Instance?.ShowNotification("❌ Not enough money for Restocker!", 2f);
                return;
            }

            restockerHired = true;
            gm.DeductMoneyLocal(RestockerCost);
            CloseHireMenu();
            RefreshHireMenuLabels();

            string restockerName = "Restocker_" + System.Guid.NewGuid().ToString()[..4];
            TycoonAPIService.Instance.HireEmployee(
                gm.CurrentPlayer.player_id,
                currentBusiness.business_id,
                new EmployeeHireRequest(restockerName, "restocker"),
                (emp) => 
                {
                    Debug.Log($"[StoreUIManager] Restocker hired and saved to backend: {emp.name}");
                    if (currentBusiness.employees == null) currentBusiness.employees = new System.Collections.Generic.List<Employee>();
                    currentBusiness.employees.Add(emp);
                    SpawnEmployeeAI("restocker");
                },
                (err) => 
                {
                    Debug.LogWarning($"[StoreUIManager] Backend hire failed: {err}");
                    restockerHired = false;
                    gm.DeductMoneyLocal(-RestockerCost);
                }
            );
        }

        private void OnHireCleanerClicked()
        {
            var gm = Managers.GameManager.Instance;
            if (gm?.CurrentPlayer == null || currentBusiness == null) return;

            if (gm.CurrentPlayer.money < CleanerCost)
            {
                UI.HUDManager.Instance?.ShowNotification("❌ Not enough money for Cleaner!", 2f);
                return;
            }

            cleanerHired = true;
            gm.DeductMoneyLocal(CleanerCost);
            CloseHireMenu();
            RefreshHireMenuLabels();

            string cleanerName = "Cleaner_" + System.Guid.NewGuid().ToString()[..4];
            TycoonAPIService.Instance.HireEmployee(
                gm.CurrentPlayer.player_id,
                currentBusiness.business_id,
                new EmployeeHireRequest(cleanerName, "cleaner"),
                (emp) => 
                {
                    Debug.Log($"[StoreUIManager] Cleaner hired and saved to backend: {emp.name}");
                    if (currentBusiness.employees == null) currentBusiness.employees = new System.Collections.Generic.List<Employee>();
                    currentBusiness.employees.Add(emp);
                    SpawnEmployeeAI("cleaner");
                },
                (err) => 
                {
                    Debug.LogWarning($"[StoreUIManager] Backend hire failed: {err}");
                    cleanerHired = false;
                    gm.DeductMoneyLocal(-CleanerCost);
                }
            );
        }


        private void SpawnEmployeeAI(string role)
        {
            if (currentStore == null)
            {
                Debug.LogError("[StoreUIManager] currentStore is null — cannot spawn employee.");
                return;
            }

            GameObject prefab = null;
            Vector3    spawnOffset = Vector3.zero;

            if (role == "cashier")
            {
                prefab      = cashierPrefab;
                spawnOffset = new Vector3(2f, 0.5f, 0f); // Near the checkout
            }
            else if (role == "restocker")
            {
                prefab      = restockerPrefab;
                spawnOffset = new Vector3(-2f, 0.5f, 0f); // Near the supply side
            }
            else if (role == "cleaner")
            {
                prefab      = cleanerPrefab;
                spawnOffset = new Vector3(0f, 0.5f, 2f);
            }

            if (prefab == null)
            {
                Debug.LogError($"[StoreUIManager] No prefab assigned for role: {role}. " +
                               "Please assign CashierPrefab / RestockerPrefab in the Inspector.");
                return;
            }

            // Spawn at entrance position + offset, parented to the store building
            Vector3 spawnPos = currentStore.GetEntrancePosition() + spawnOffset;
            GameObject empObj = Instantiate(prefab, spawnPos, Quaternion.identity, currentStore.transform);
            empObj.name = $"{role}_{System.Guid.NewGuid().ToString()[..4]}";

            // Initialize CleanerAI if applicable
            if (role == "cleaner")
            {
                var cleanerAI = empObj.GetComponent<CleanerAI>();
                var gm = Managers.GameManager.Instance;
                cleanerAI?.Initialize(gm.CurrentPlayer.player_id, currentBusiness.business_id);
            }

            // Fetch the newly hired Employee data from the currentBusiness list
            // (Assuming it's the last one added to the list)
            if (currentBusiness.employees != null && currentBusiness.employees.Count > 0)
            {
                Employee newlyHiredData = currentBusiness.employees[^1]; // Get last item
                
                var interactionManager = empObj.GetComponent<EmployeeInteractionManager>();
                if (interactionManager != null)
                {
                    interactionManager.Initialize(newlyHiredData, currentBusiness.business_id);
                }
            }

            Debug.Log($"[StoreUIManager] Spawned {role} AI at {spawnPos}");

        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region UI Helpers

        private void RefreshHireMenuLabels()
        {
            var gm = Managers.GameManager.Instance;
            float money = gm?.CurrentPlayer?.money ?? 0f;

            if (cashierStatusText != null)
            {
                if (cashierHired)
                    cashierStatusText.text = "✅ Already Hired";
                else
                    cashierStatusText.text = money >= CashierCost ? "✔ Can Afford" : "✖ Not enough money";

                cashierStatusText.color = (cashierHired || money >= CashierCost) ? Color.green : Color.red;
            }

            if (restockerStatusText != null)
            {
                if (restockerHired)
                    restockerStatusText.text = "✅ Already Hired";
                else
                    restockerStatusText.text = money >= RestockerCost ? "✔ Can Afford" : "✖ Not enough money";

                restockerStatusText.color = (restockerHired || money >= RestockerCost) ? Color.green : Color.red;
            }

            if (cleanerStatusText != null)
            {
                if (cleanerHired)
                    cleanerStatusText.text = "✅ Already Hired";
                else
                    cleanerStatusText.text = money >= CleanerCost ? "✔ Can Afford" : "✖ Not enough money";
                cleanerStatusText.color = (cleanerHired || money >= CleanerCost) ? Color.green : Color.red;
            }
            if (hireCleanerButton != null) hireCleanerButton.interactable = !cleanerHired;

            // Update store rating display:
            if (storeRatingText != null && currentBusiness != null)
                storeRatingText.text = currentBusiness.RatingText;

            // Disable buttons if already hired
            if (hireCashierButton  != null) hireCashierButton.interactable  = !cashierHired;
            if (hireRestockerButton != null) hireRestockerButton.interactable = !restockerHired;
        }

        #endregion
    }
}
