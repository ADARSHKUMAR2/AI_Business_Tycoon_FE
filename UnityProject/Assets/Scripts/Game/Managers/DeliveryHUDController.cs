using System;
using System.Collections.Generic;
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
        private bool expressDeliveryRequestInProgress;

        private static readonly HashSet<string> InFlightExpressDeliveryKeys = new HashSet<string>();
        private const string PendingExpressDeliveryKeyPrefix = "pending_express_delivery_";

        private DateTime? supplyWindowEndUtc;
        private DateTime? nextDeliveryUtc;
        private bool truckActive = false;
        private bool statusRequestInProgress;
        private bool deliveryBoundaryHandled;
        private bool nextDeliveryBoundaryHandled;
        private float nextResumeRefreshTime;

        private const float ResumeRefreshCooldownSeconds = 2f;

        private void Awake()
        {
            if (panel != null)
                panel.SetActive(false);

            if (expressButton != null)
                expressButton.onClick.AddListener(OnExpressDeliveryClicked);
        }

        private void Update()
        {
            UpdateCountdownText();
            HandleExpiredSupplyWindow();
            HandleNextDeliveryBoundary();
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

            supplyWindowEndUtc = null;
            nextDeliveryUtc = null;
            truckActive = false;
            statusRequestInProgress = false;
            deliveryBoundaryHandled = false;
            nextDeliveryBoundaryHandled = false;

            if (panel != null)
                panel.SetActive(false);

            if (countdownText != null)
                countdownText.text = "Next truck in 00:00";
        }

        private void FetchStatus()
        {
            if (TycoonAPIService.Instance == null || statusRequestInProgress ||
                string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(businessId))
                return;

            statusRequestInProgress = true;

            TycoonAPIService.Instance.GetDeliveryStatus(
                playerId,
                businessId,
                (DeliveryStatusResponse response) =>
                {
                    statusRequestInProgress = false;
                    ApplyDeliveryStatus(response);
                },
                (string error) =>
                {
                    statusRequestInProgress = false;
                    Debug.LogError("[DeliveryHUDController] Delivery status failed: " + error);
                }
            );
        }

        private void ApplyDeliveryStatus(DeliveryStatusResponse response)
        {
            if (response == null)
                return;

            deliveryBoundaryHandled = false;
            nextDeliveryBoundaryHandled = false;

            if (currentStore != null)
            {
                currentStore.ApplyDeliveryStatus(response);
            }

            truckActive = response.supply_available;

            if (response.supply_available)
            {
                supplyWindowEndUtc = DateTime.UtcNow.AddSeconds(Mathf.Max(0, response.supply_window_remaining));
                nextDeliveryUtc = ParseUtc(response.next_delivery_at);
            }
            else
            {
                nextDeliveryUtc = ParseUtc(response.next_delivery_at) ??
                    DateTime.UtcNow.AddSeconds(Mathf.Max(0, response.seconds_until_next));
                supplyWindowEndUtc = null;
            }

            if (expressButton != null)
            {
                bool hasPendingRequest = HasPendingExpressDeliveryRequest();
                expressButton.interactable =
                    (!response.supply_available || hasPendingRequest) &&
                    !expressDeliveryRequestInProgress;
            }
        }

        private void HandleExpiredSupplyWindow()
        {
            if (!truckActive || !supplyWindowEndUtc.HasValue || deliveryBoundaryHandled)
                return;

            if (DateTime.UtcNow < supplyWindowEndUtc.Value)
                return;

            deliveryBoundaryHandled = true;
            truckActive = false;
            supplyWindowEndUtc = null;
            currentStore?.HideSupplyZones();
            FetchStatus();
        }

        private void HandleNextDeliveryBoundary()
        {
            if (truckActive || !nextDeliveryUtc.HasValue || nextDeliveryBoundaryHandled)
                return;

            if (DateTime.UtcNow < nextDeliveryUtc.Value)
                return;

            nextDeliveryBoundaryHandled = true;
            FetchStatus();
        }

        private void OnApplicationPause(bool isPaused)
        {
            if (!isPaused)
                RequestResumeRefresh();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                RequestResumeRefresh();
        }

        private void RequestResumeRefresh()
        {
            if (Time.unscaledTime < nextResumeRefreshTime)
                return;

            nextResumeRefreshTime = Time.unscaledTime + ResumeRefreshCooldownSeconds;
            FetchStatus();
        }

        private void UpdateCountdownText()
        {
            if (countdownText == null)
                return;

            DateTime now = DateTime.UtcNow;
            if (truckActive && supplyWindowEndUtc.HasValue)
            {
                int remaining = Mathf.Max(0, (int)Math.Ceiling((supplyWindowEndUtc.Value - now).TotalSeconds));
                countdownText.text = "Truck here! " + FormatSeconds(remaining);
                return;
            }

            if (nextDeliveryUtc.HasValue)
            {
                int remaining = Mathf.Max(0, (int)Math.Ceiling((nextDeliveryUtc.Value - now).TotalSeconds));
                countdownText.text = "Next truck in " + FormatSeconds(remaining);
            }
        }

        private static DateTime? ParseUtc(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !DateTime.TryParse(
                    value,
                    null,
                    System.Globalization.DateTimeStyles.AssumeUniversal |
                    System.Globalization.DateTimeStyles.AdjustToUniversal,
                    out DateTime parsed))
            {
                return null;
            }

            return parsed;
        }

        private void OnExpressDeliveryClicked()
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(businessId))
            {
                Debug.LogError("[DeliveryHUDController] Missing IDs for express delivery.");
                return;
            }

            if (expressDeliveryRequestInProgress)
                return;

            string requestPlayerId = playerId;
            string requestBusinessId = businessId;
            string preferenceKey = GetPendingExpressDeliveryPreferenceKey(
                requestPlayerId,
                requestBusinessId);
            string idempotencyKey = PlayerPrefs.GetString(preferenceKey, string.Empty);

            if (string.IsNullOrEmpty(idempotencyKey))
            {
                idempotencyKey = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(preferenceKey, idempotencyKey);
                PlayerPrefs.Save();
            }

            if (!InFlightExpressDeliveryKeys.Add(idempotencyKey))
                return;

            expressDeliveryRequestInProgress = true;
            if (expressButton != null)
                expressButton.interactable = false;

            string requestKey = idempotencyKey;
            TycoonAPIService.Instance.RequestExpressDelivery(
                requestPlayerId,
                requestBusinessId,
                requestKey,
                (BusinessData business) =>
                {
                    InFlightExpressDeliveryKeys.Remove(requestKey);
                    expressDeliveryRequestInProgress = false;

                    if (PlayerPrefs.GetString(preferenceKey, string.Empty) == requestKey)
                    {
                        PlayerPrefs.DeleteKey(preferenceKey);
                        PlayerPrefs.Save();
                    }

                    Debug.Log("[DeliveryHUDController] Express restock successful.");
                    if (playerId == requestPlayerId && businessId == requestBusinessId)
                    {
                        FetchStatus();
                    }
                },
                (string error) =>
                {
                    InFlightExpressDeliveryKeys.Remove(requestKey);
                    expressDeliveryRequestInProgress = false;

                    Debug.LogError("[DeliveryHUDController] Express restock failed: " + error);
                    if (playerId == requestPlayerId && businessId == requestBusinessId &&
                        expressButton != null)
                    {
                        expressButton.interactable = !truckActive ||
                            HasPendingExpressDeliveryRequest();
                    }
                }
            );
        }

        private bool HasPendingExpressDeliveryRequest()
        {
            if (string.IsNullOrEmpty(playerId) || string.IsNullOrEmpty(businessId))
                return false;

            string preferenceKey = GetPendingExpressDeliveryPreferenceKey(playerId, businessId);
            return !string.IsNullOrEmpty(PlayerPrefs.GetString(preferenceKey, string.Empty));
        }

        private static string GetPendingExpressDeliveryPreferenceKey(
            string requestPlayerId,
            string requestBusinessId)
        {
            // Length-prefix the player ID so combined IDs cannot collide on delimiters.
            return PendingExpressDeliveryKeyPrefix + requestPlayerId.Length + "_" +
                requestPlayerId + requestBusinessId;
        }

        private string FormatSeconds(int value)
        {
            int minutes = value / 60;
            int seconds = value % 60;
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }
}
