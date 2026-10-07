using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Managers;

namespace AIBusinessTycoon.UI
{
    /// <summary>
    /// Displays an active franchise tournament event as a banner at the top of the screen.
    /// Shows event details, countdown timer, and join button.
    /// </summary>
    public class EventBannerUI : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject bannerPanel;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI detailsText;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Button joinButton;
        [SerializeField] private TextMeshProUGUI joinButtonText;
        
        [Header("Colors")]
        [SerializeField] private Color activeColor = new Color(0.15f, 0.9f, 0.27f);
        [SerializeField] private Color upcomingColor = new Color(1f, 0.76f, 0.03f);
        [SerializeField] private Color registeredColor = new Color(0.4f, 0.4f, 0.4f);
        
        [Header("Animation")]
        [SerializeField] private float slideSpeed = 3f;
        
        private EventResponse currentEvent;
        private bool isVisible;
        private float targetAlpha;
        
        private void Start()
        {
            if (bannerPanel != null)
                bannerPanel.SetActive(false);
            
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            
            if (joinButton != null)
                joinButton.onClick.AddListener(OnJoinButtonClicked);
            
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnEventUpdated += OnEventUpdated;
                EventManager.Instance.OnEventEnded += OnEventEnded;
            }
        }
        
        private void OnDestroy()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnEventUpdated -= OnEventUpdated;
                EventManager.Instance.OnEventEnded -= OnEventEnded;
            }
            
            if (joinButton != null)
                joinButton.onClick.RemoveListener(OnJoinButtonClicked);
        }
        
        private void Update()
        {
            if (currentEvent != null && isVisible)
            {
                UpdateTimer();
            }
            
            if (canvasGroup != null && canvasGroup.alpha != targetAlpha)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, slideSpeed * Time.deltaTime);
            }
        }
        
        private void OnEventUpdated(EventResponse eventData)
        {
            currentEvent = eventData;
            ShowBanner();
            UpdateUI();
        }
        
        private void OnEventEnded()
        {
            HideBanner();
        }
        
        private void ShowBanner()
        {
            if (bannerPanel != null)
                bannerPanel.SetActive(true);
            
            isVisible = true;
            targetAlpha = 1f;
        }
        
        private void HideBanner()
        {
            isVisible = false;
            targetAlpha = 0f;
            
            if (bannerPanel != null && canvasGroup != null && canvasGroup.alpha <= 0.01f)
                bannerPanel.SetActive(false);
        }
        
        private void UpdateUI()
        {
            if (currentEvent == null) return;
            
            // Update title
            if (titleText != null)
                titleText.text = $"{currentEvent.franchise_name.ToUpper()} TOURNAMENT";
            
            // Update status badge
            if (statusText != null)
            {
                if (currentEvent.IsCompleted)
                {
                    statusText.text = "● COMPLETED";
                    statusText.color = registeredColor; // Gray
                }
                else
                {
                    statusText.text = currentEvent.IsActive ? "● ACTIVE" : "● UPCOMING";
                    statusText.color = currentEvent.IsActive ? activeColor : upcomingColor;
                }
            }
            
            // Update details (entry fee & participants)
            if (detailsText != null)
            {
                detailsText.text = $"Entry: ₹{currentEvent.entry_fee:N0} | " +
                                  $"Players: {currentEvent.participant_count}/{currentEvent.max_winners}";
            }
            
            // Update join button
            UpdateJoinButton();
        }
        
        private void UpdateJoinButton()
        {
            if (joinButton == null || joinButtonText == null) return;

            if (currentEvent.IsCompleted)
            {
                joinButtonText.text = "VIEW RESULTS";
                joinButton.interactable = true; // Click to open leaderboard
                if (joinButton.targetGraphic != null)
                    joinButton.targetGraphic.color = upcomingColor; // Yellow
                return;
            }
            
            var gm = GameManager.Instance;
            if (currentEvent.is_registered)
            {
                bool isPlaced = false;
                if (gm != null && gm.CurrentPlayer != null)
                {
                    foreach (var biz in gm.CurrentPlayer.businesses)
                    {
                        if (biz.is_event_business && biz.event_id == currentEvent.event_id)
                        {
                            isPlaced = true;
                            break;
                        }
                    }
                }

                if (isPlaced)
                {
                    joinButtonText.text = "✓ STORE PLACED";
                    joinButton.interactable = false;
                    if (joinButton.targetGraphic != null)
                        joinButton.targetGraphic.color = registeredColor;
                }
                else
                {
                    joinButtonText.text = "PLACE STORE";
                    joinButton.interactable = true;
                    if (joinButton.targetGraphic != null)
                        joinButton.targetGraphic.color = upcomingColor; // Actionable
                }
            }
            else
            {
                bool canAfford = gm != null && gm.CurrentPlayer != null &&
                                gm.CurrentPlayer.money >= currentEvent.entry_fee;

                joinButtonText.text = canAfford ? "JOIN NOW" : "INSUFFICIENT FUNDS";
                joinButton.interactable = canAfford;
                if (joinButton.targetGraphic != null)
                    joinButton.targetGraphic.color = activeColor;
            }
        }
        
        private void UpdateTimer()
        {
            if (timerText == null || currentEvent == null) return;
            
            int remaining = currentEvent.GetRemainingSeconds();
            
            if (remaining <= 0)
            {
                timerText.text = "Event Ended";
                timerText.color = Color.red;
            }
            else
            {
                timerText.text = $"Ends in: {currentEvent.GetFormattedTimeRemaining()}";
                timerText.color = remaining < 300 ? Color.yellow : Color.white;
            }
        }
        
        private void OnJoinButtonClicked()
        {
            if (currentEvent == null) return;

            if (currentEvent.IsCompleted)
            {
                var leaderboard = FindObjectOfType<LeaderboardPanel>(true);
                if (leaderboard != null)
                {
                    leaderboard.OpenPanel();
                }
                return;
            }

            if (currentEvent.is_registered)
            {
                joinButton.interactable = false;
                joinButtonText.text = "PLACING...";
                
                EventManager.Instance?.RetryEventPlacement((success) =>
                {
                    UpdateJoinButton();
                });
                return;
            }

            if (!currentEvent.CanJoin) return;

            joinButton.interactable = false;
            joinButtonText.text = "JOINING...";

            EventManager.Instance?.RegisterForEvent((success) =>
            {
                if (success)
                {
                    Debug.Log("[EventBannerUI] Successfully joined event");
                }
                UpdateJoinButton();
            });
        }
    }
}
