using System;
using UnityEngine;
using UnityEngine.UI;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;

namespace AIBusinessTycoon.Managers
{
    public class DeliveryHUDController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private GameObject panel;
        [SerializeField] private Text countdownText;
        [SerializeField] private Button expressButton;

        private StoreInteractionManager currentStore;
        private string playerId;
        private string businessId;

        private float activeWindowTimer = 0f;
        private float nextDeliveryTimer = 0f;
        private bool truckActive = false;

        private void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            if (expressButton != null)
                expressButton.onClick.AddListener(OnExpressDeliveryClicked);
        }

        private void Update()
        {
            if (truckActive)
            {
                activeWindowTimer = Mathf.Max(0f, activeWindowTimer - Time.deltaTime);
                if (countdownText != null)
                {
                    countdownText.text = "Truck here! " + FormatSeconds(Mathf.CeilToInt(activeWindowTimer));
                }
            }
            else
            {
                nextDeliveryTimer = Mathf.Max(0f, nextDeliveryTimer - Time.deltaTime);
                if (countdownText != null)
                {
                    countdownText.text = "Next truck in " + FormatSeconds(Mathf.CeilToInt(nextDeliveryTimer));
                }
            }
        }

        public void BindToStore(StoreInteractionManager store)
        {
            currentStore = store;

            if (store == null || store.BusinessData == null)
            {
                Debug.LogWarning("[DeliveryHUDController] Cannot bind: store or BusinessData is null.");
                return;
            }

            playerId = GameManager.Instance != null ? GameManager.Instance.CurrentPlayerId : "";
            businessId = store.BusinessData.business_id;

            if (panel != null)
                panel.SetActive(true);

            if (store.CurrentDeliveryStatus != null)
            {
                ApplyDeliveryStatus(store.CurrentDeliveryStatus);
            }
        }

        public void Unbind()
        {
            currentStore = null;
            playerId = "";
            businessId = "";

            activeWindowTimer = 0f;
            nextDeliveryTimer = 0f;
            truckActive = false;

            if (panel != null)
                panel.SetActive(false);

            if (countdownText != null)
                countdownText.text = "Next truck in 00:00";
        }

        private void FetchStatus()
        {
            if (TycoonAPIService.Instance == null)
                return;

            TycoonAPIService.Instance.GetDeliveryStatus(
                playerId,
                businessId,
                (DeliveryStatusResponse response) =>
                {
                    ApplyDeliveryStatus(response);
                },
                (string error) =>
                {
                    Debug.LogError("[DeliveryHUDController] Delivery status failed: " + error);
                }
            );
        }

        private void ApplyDeliveryStatus(DeliveryStatusResponse response)
        {
            if (response == null)
                return;

            if (currentStore != null)
            {
                currentStore.ApplyDeliveryStatus(response);
            }

            truckActive = response.supply_available;

            if (response.supply_available)
            {
                activeWindowTimer = response.supply_window_remaining;
            }
            else
            {
                nextDeliveryTimer = response.seconds_until_next;
            }

            if (expressButton != null)
            {
                expressButton.interactable = !response.supply_available;
            }
        }

        private void OnExpressDeliveryClicked()
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(businessId))
            {
                Debug.LogError("[DeliveryHUDController] Missing IDs for express delivery.");
                return;
            }

            TycoonAPIService.Instance.RequestExpressDelivery(
                playerId,
                businessId,
                (BusinessData business) =>
                {
                    Debug.Log("[DeliveryHUDController] Express restock successful.");
                    FetchStatus();
                },
                (string error) =>
                {
                    Debug.LogError("[DeliveryHUDController] Express restock failed: " + error);
                }
            );
        }

        private string FormatSeconds(int value)
        {
            int minutes = value / 60;
            int seconds = value % 60;
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
