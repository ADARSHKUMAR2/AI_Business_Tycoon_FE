using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.UI
{
    /// <summary>
    /// UI shown when the player is INSIDE a store.
    /// Shows the store name, a joystick, and an Exit button.
    /// Completely separate from the Macro HUD.
    /// </summary>
    public class StoreUIManager : MonoBehaviour
    {
        public static StoreUIManager Instance { get; private set; }

        [Header("Panels")]
        [SerializeField] private GameObject storeUIPanel;

        [Header("Store Info")]
        [SerializeField] private TextMeshProUGUI storeNameText;
        [SerializeField] private TextMeshProUGUI storeTypeText;

        [Header("Joystick")]
        [SerializeField] private GameObject joystickPanel;
        [SerializeField] private JoystickController joystick;

        [Header("Buttons")]
        [SerializeField] private Button exitButton;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);

            HideStoreUI();
        }

        public void ShowStoreUI(BusinessData business)
        {
            if (storeUIPanel != null) storeUIPanel.SetActive(true);
            if (joystickPanel != null) joystickPanel.SetActive(true);

            if (business != null)
            {
                if (storeNameText != null) storeNameText.text = business.name;
                if (storeTypeText != null) storeTypeText.text = business.business_type.ToUpper();
            }

            Debug.Log("[StoreUIManager] Store UI shown");
        }

        public void HideStoreUI()
        {
            if (storeUIPanel != null) storeUIPanel.SetActive(false);
            if (joystickPanel != null) joystickPanel.SetActive(false);

            Debug.Log("[StoreUIManager] Store UI hidden");
        }

        private void OnExitClicked()
        {
            Managers.GameManager.Instance?.ExitStore();
        }
    }
}
