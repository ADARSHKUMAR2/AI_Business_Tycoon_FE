using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;
using AIBusinessTycoon.Managers;

namespace AIBusinessTycoon.UI
{
    public class EmployeeUpgradeUIManager : MonoBehaviour
    {
        public static EmployeeUpgradeUIManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] public GameObject popupPanel;

        [Header("Employee Info")]
        [SerializeField] public TextMeshProUGUI nameText;
        [SerializeField] public TextMeshProUGUI roleText;

        [Header("Speed Upgrade")]
        [SerializeField] public TextMeshProUGUI speedValueText;
        [SerializeField] public Button upgradeSpeedButton;
        [SerializeField] public TextMeshProUGUI speedCostText;

        [Header("Carry Capacity Upgrade")]
        [SerializeField] public TextMeshProUGUI capacityValueText;
        [SerializeField] public Button upgradeCapacityButton;
        [SerializeField] public TextMeshProUGUI capacityCostText;

        [Header("Controls")]
        [SerializeField] public Button closeButton;

        private EmployeeInteractionManager activeInteractionManager;
        private Employee currentEmployee;
        private string currentBusinessId;

        private const int UPGRADE_COST_BASE = 10;
        private const int UPGRADE_STAT_INC = 5;
        private const int UPGRADE_CARRY_INC = 2;
        private const int MAX_SPEED = 100;
        private const int MAX_CARRY = 20;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (closeButton != null) closeButton.onClick.AddListener(CloseUI);
            if (upgradeSpeedButton != null) upgradeSpeedButton.onClick.AddListener(() => RequestUpgrade("speed"));
            if (upgradeCapacityButton != null) upgradeCapacityButton.onClick.AddListener(() => RequestUpgrade("carry_capacity"));

            popupPanel?.SetActive(false);
        }

        // Changed to accept the InteractionManager instead of raw data
        public void OpenUpgradeUI(EmployeeInteractionManager interactionMgr, string businessId)
        {
            activeInteractionManager = interactionMgr;
            currentEmployee          = interactionMgr.employeeData;
            currentBusinessId        = businessId;
            
            RefreshUI();
            popupPanel?.SetActive(true);
            Managers.PlayerController.Instance?.SetJoystickInput(Vector2.zero);
        }

        public void CloseUI()
        {
            popupPanel?.SetActive(false);
            activeInteractionManager = null;
            currentEmployee = null;
        }

        private void RefreshUI()
        {
            if (currentEmployee == null) return;

            var gm = Managers.GameManager.Instance;
            float money = gm?.CurrentPlayer?.money ?? 0f;

            if (nameText != null) nameText.text = currentEmployee.name;
            if (roleText != null) roleText.text = currentEmployee.role.ToUpper();

            // ── Speed ──
            int currentSpeed = currentEmployee.stats.speed;
            if (speedValueText != null) speedValueText.text = $"{currentSpeed} / {MAX_SPEED}";
            
            if (currentSpeed >= MAX_SPEED)
            {
                if (speedCostText != null) speedCostText.text = "MAX";
                if (upgradeSpeedButton != null) upgradeSpeedButton.interactable = false;
            }
            else
            {
                float cost = UPGRADE_COST_BASE * currentSpeed * UPGRADE_STAT_INC;
                if (speedCostText != null) speedCostText.text = $"Rs. {cost:N0}";
                if (upgradeSpeedButton != null) upgradeSpeedButton.interactable = (money >= cost);
            }

            // ── Carry Capacity ──
            int currentCap = currentEmployee.stats.carry_capacity;
            if (capacityValueText != null) capacityValueText.text = $"{currentCap} / {MAX_CARRY}";

            if (currentCap >= MAX_CARRY)
            {
                if (capacityCostText != null) capacityCostText.text = "MAX";
                if (upgradeCapacityButton != null) upgradeCapacityButton.interactable = false;
            }
            else
            {
                float cost = UPGRADE_COST_BASE * currentCap * UPGRADE_CARRY_INC;
                if (capacityCostText != null) capacityCostText.text = $"Rs. {cost:N0}";
                if (upgradeCapacityButton != null) upgradeCapacityButton.interactable = (money >= cost);
            }
        }

        private void RequestUpgrade(string statType)
        {
            var gm = Managers.GameManager.Instance;
            if (gm == null || currentEmployee == null) return;

            upgradeSpeedButton.interactable = false;
            upgradeCapacityButton.interactable = false;

            var request = new EmployeeUpgradeRequest(statType);

            TycoonAPIService.Instance.UpgradeEmployee(
                gm.CurrentPlayer.player_id, 
                currentBusinessId, 
                currentEmployee.employee_id, 
                request,
                (updatedEmp) => 
                {
                    Debug.Log($"[EmployeeUpgradeUI] Successfully upgraded {statType}");
                    
                    // Deduct money locally
                    float cost = (statType == "speed") 
                        ? UPGRADE_COST_BASE * currentEmployee.stats.speed * UPGRADE_STAT_INC 
                        : UPGRADE_COST_BASE * currentEmployee.stats.carry_capacity * UPGRADE_CARRY_INC;
                    gm.DeductMoneyLocal(cost);

                    // 1. UPDATE THE LOCAL OBJECT IN MEMORY!
                    // This directly updates the reference held by the 3D Avatar and GameManager
                    currentEmployee.stats.speed          = updatedEmp.stats.speed;
                    currentEmployee.stats.carry_capacity = updatedEmp.stats.carry_capacity;
                    currentEmployee.salary_per_day       = updatedEmp.salary_per_day;

                    // 2. TELL THE 3D AVATAR TO WALK FASTER / CARRY MORE
                    if (activeInteractionManager != null)
                    {
                        activeInteractionManager.ApplyPhysicalStats();
                    }

                    HUDManager.Instance?.ShowNotification("📈 Upgrade Successful!", 2f);
                    RefreshUI();
                },
                (err) => 
                {
                    Debug.LogWarning($"[EmployeeUpgradeUI] Failed to upgrade: {err}");
                    HUDManager.Instance?.ShowNotification("❌ Upgrade Failed!", 2f);
                    RefreshUI(); 
                }
            );
        }
    }
}
