using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;
using System.Collections.Generic;

namespace AIBusinessTycoon.UI
{
    /// <summary>
    /// UI shown when the player is INSIDE a store.
    /// Phase 2: Added Hire Staff panel with Cashier and Restocker hiring buttons.
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
        [SerializeField] private JoystickController joystick; // Fixed: UI namespace

        // ── Main Buttons ─────────────────────────────────────────────────────
        [Header("Buttons")]
        [SerializeField] private Button exitButton;
        [SerializeField] private Button hireStaffButton;     // Phase 2: Opens hire menu

        // ── Hire Menu Buttons ─────────────────────────────────────────────────
        [Header("Hire Menu Buttons")]
        [SerializeField] private Button hireCashierButton;   // Rs. 1,000
        [SerializeField] private Button hireRestockerButton; // Rs. 1,500
        [SerializeField] private Button closeHireMenuButton;

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

        // ── Internal State ─────────────────────────────────────────────────────
        private BusinessData currentBusiness;
        private Managers.StoreInteractionManager currentStore; // Fixed: Managers namespace

        // Track which employee types have been hired this session
        // (prevents hiring duplicates; could be expanded to allow multiples later)
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
            if (exitButton        != null) exitButton.onClick.AddListener(OnExitClicked);
            if (hireStaffButton   != null) hireStaffButton.onClick.AddListener(OnHireStaffClicked);
            if (closeHireMenuButton != null) closeHireMenuButton.onClick.AddListener(CloseHireMenu);
            if (hireCashierButton  != null) hireCashierButton.onClick.AddListener(() => OnHireClicked("cashier", CashierCost));
            if (hireRestockerButton != null) hireRestockerButton.onClick.AddListener(() => OnHireClicked("restocker", RestockerCost));

            HideStoreUI();
        }

        // ──────────────────────────────────────────────────────────────────────
        #region Show / Hide

        /// <summary>
        /// Called by GameManager when the player enters a store.
        /// Phase 2: also accepts the StoreInteractionManager reference for spawning employees.
        /// </summary>
        public void ShowStoreUI(BusinessData business, Managers.StoreInteractionManager store = null)
        {
            currentBusiness = business;
            currentStore    = store;

            if (storeUIPanel  != null) storeUIPanel.SetActive(true);
            if (joystickPanel != null) joystickPanel.SetActive(true);
            if (hireMenuPanel != null) hireMenuPanel.SetActive(false); // start hidden

            if (business != null)
            {
                if (storeNameText != null) storeNameText.text = business.name;
                if (storeTypeText != null) storeTypeText.text = business.business_type.ToUpper();
            }

            // Reset hire state for this store visit
            cashierHired   = false;
            restockerHired = false;

            if (business != null && business.employees != null)
            {
                foreach (var emp in business.employees)
                {
                    if (emp.role == "cashier") cashierHired = true;
                    if (emp.role == "restocker") restockerHired = true;
                }
            }

            Debug.Log("[StoreUIManager] Store UI shown");
        }

        public void HideStoreUI()
        {
            if (storeUIPanel  != null) storeUIPanel.SetActive(false);
            if (joystickPanel != null) joystickPanel.SetActive(false);
            if (hireMenuPanel != null) hireMenuPanel.SetActive(false);

            currentBusiness = null;
            currentStore    = null;

            Debug.Log("[StoreUIManager] Store UI hidden");
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Button Handlers

        private void OnExitClicked()
        {
            Managers.GameManager.Instance?.ExitStore();
        }

        private void OnHireStaffClicked()
        {
            if (hireMenuPanel == null) return;

            // Refresh affordability labels each time the menu opens
            RefreshHireMenuLabels();
            hireMenuPanel.SetActive(true);
        }

        private void CloseHireMenu()
        {
            if (hireMenuPanel != null) hireMenuPanel.SetActive(false);
        }

        /// <summary>
        /// Central hiring handler called by both Cashier and Restocker buttons.
        /// </summary>
        private void OnHireClicked(string role, float cost)
        {
            var gm = Managers.GameManager.Instance;
            if (gm == null) return;

            // ── Guard: already hired this type? ──
            if (role == "cashier"   && cashierHired)
            {
                HUDManager.Instance?.ShowNotification("Cashier already hired!", 2f); // Fixed HUD namespace
                return;
            }
            if (role == "restocker" && restockerHired)
            {
                HUDManager.Instance?.ShowNotification("Restocker already hired!", 2f); // Fixed HUD namespace
                return;
            }

            // ── Guard: can afford? ──
            if (!gm.CanAfford(cost))
            {
                HUDManager.Instance?.ShowNotification($"Not enough money! Need Rs.{cost:N0}", 2f); // Fixed HUD namespace
                return;
            }

            // ── Guard: need a valid business to save to backend ──
            if (currentBusiness == null)
            {
                Debug.LogError("[StoreUIManager] No current business set — cannot hire.");
                return;
            }

            // ── Deduct money immediately (optimistic) ──
            gm.DeductMoneyLocal(cost);
            HUDManager.Instance?.ShowNotification($"Hiring {role}... Rs.{cost:N0} paid!", 2f); // Fixed HUD namespace

            // ── Spawn the AI in the scene right now ──
            SpawnEmployeeAI(role);

            // ── Mark as hired ──
            if (role == "cashier")   cashierHired   = true;
            if (role == "restocker") restockerHired = true;

            // Optimistically add to local business data so it persists until next refresh
            if (currentBusiness.employees == null) currentBusiness.employees = new List<Employee>();
            currentBusiness.employees.Add(new Employee { role = role, name = "New Hire" });

            // ── Sync to backend ──
            string[] names = { "Ravi", "Priya", "Amit", "Sunita", "Kiran", "Deepa" };
            string randomName = names[Random.Range(0, names.Length)] + " " + role[0].ToString().ToUpper() + ".";

            var request = new EmployeeHireRequest(randomName, role);
            TycoonAPIService.Instance.HireEmployee(
                gm.CurrentPlayer.player_id,
                currentBusiness.business_id,
                request,
                (emp) => Debug.Log($"[StoreUIManager] Backend confirmed hire: {emp.name}"),
                (err) => Debug.LogWarning($"[StoreUIManager] Backend hire failed (local hire still active): {err}")
            );

            // Close the menu
            CloseHireMenu();

            // Update labels for next open
            RefreshHireMenuLabels();
        }

        #endregion

        // ──────────────────────────────────────────────────────────────────────
        #region Employee Spawning

        /// <summary>
        /// Instantiates the employee prefab inside the current store building.
        /// The employee's own AI script takes over from there.
        /// </summary>
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

            if (prefab == null)
            {
                Debug.LogError($"[StoreUIManager] No prefab assigned for role: {role}. " +
                               "Please assign CashierPrefab / RestockerPrefab in the Inspector.");
                return;
            }

            // Spawn at entrance position + offset, parented to the store building
            Vector3 spawnPos = currentStore.GetEntrancePosition() + spawnOffset;
            GameObject emp   = Instantiate(prefab, spawnPos, Quaternion.identity, currentStore.transform);
            emp.name         = $"{role}_{System.Guid.NewGuid().ToString()[..4]}";

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

            // Disable buttons if already hired
            if (hireCashierButton  != null) hireCashierButton.interactable  = !cashierHired;
            if (hireRestockerButton != null) hireRestockerButton.interactable = !restockerHired;
        }

        #endregion
    }
}
