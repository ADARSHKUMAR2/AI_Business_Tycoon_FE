using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Managers;
using AIBusinessTycoon.Multiplayer;
using AIBusinessTycoon.Services;

namespace AIBusinessTycoon.UI
{
    /// <summary>
    /// Phase 5 competitive leaderboard panel.
    /// Subscribes to RealtimeLeaderboardService and renders live rankings.
    /// Wire all references via: AI Business Tycoon / Setup Leaderboard UI (Phase 5)
    /// </summary>
    public class LeaderboardPanel : MonoBehaviour
    {
        // ── Panel root ────────────────────────────────────────────────────────
        [Header("Panel Root")]
        [SerializeField] private GameObject      panelRoot;
        [SerializeField] private TextMeshProUGUI titleLabel;
        [SerializeField] private TextMeshProUGUI connectionStatusLabel;
        [SerializeField] private TextMeshProUGUI lastUpdatedLabel;
        [SerializeField] private Button          closeButton;

        // ── Metric tabs ───────────────────────────────────────────────────────
        [Header("Metric Tabs (Hidden in Event Mode)")]
        [SerializeField] private Button tabNetWorth;
        [SerializeField] private Button tabRevenue;
        [SerializeField] private Button tabCustomers;
        [SerializeField] private Image  tabNetWorthUnderline;
        [SerializeField] private Image  tabRevenueUnderline;
        [SerializeField] private Image  tabCustomersUnderline;

        // ── Row list ──────────────────────────────────────────────────────────
        [Header("Row List")]
        [SerializeField] private Transform       rowContainer;
        [SerializeField] private GameObject      rowPrefab;
        [SerializeField] private TextMeshProUGUI emptyLabel;

        // ── Toggle (open) button shown on HUD ─────────────────────────────────
        [Header("HUD Toggle Button")]
        [SerializeField] private Button          openButton;
        [SerializeField] private TextMeshProUGUI openButtonText;

        // ── Internal state ────────────────────────────────────────────────────
        private LeaderboardUpdateEvent latestData;
        private readonly List<GameObject> spawnedRows = new List<GameObject>();

        // Colour constants
        private static readonly Color ColGold        = new Color(1.00f, 0.84f, 0.20f, 1.00f);
        private static readonly Color ColSilver      = new Color(0.78f, 0.78f, 0.78f, 1.00f);
        private static readonly Color ColBronze      = new Color(0.80f, 0.50f, 0.25f, 1.00f);
        private static readonly Color ColDefault     = new Color(1.00f, 1.00f, 1.00f, 0.90f);
        private static readonly Color ColLive        = new Color(0.27f, 0.90f, 0.45f, 1.00f);
        private static readonly Color ColConnecting  = new Color(1.00f, 0.65f, 0.10f, 1.00f);

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Start()
        {
            if (openButton   != null) openButton.onClick.AddListener(OpenPanel);
            if (closeButton  != null) closeButton.onClick.AddListener(ClosePanel);

            // Hide tabs since we only track Event Revenue now
            if (tabNetWorth  != null) tabNetWorth.gameObject.SetActive(false);
            if (tabRevenue   != null) tabRevenue.gameObject.SetActive(false);
            if (tabCustomers != null) tabCustomers.gameObject.SetActive(false);

            if (panelRoot != null) panelRoot.SetActive(false);

            if (RealtimeLeaderboardService.Instance != null)
                RealtimeLeaderboardService.Instance.LeaderboardUpdated += OnLeaderboardUpdated;

            // Subscribe to EventManager so the button reflects event availability
            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnEventUpdated += OnEventUpdated;
                EventManager.Instance.OnEventEnded   += OnEventEnded;
            }

            // Set the initial button state — disabled until an event is detected
            RefreshOpenButton();

            RefreshConnectionStatus();
            StartCoroutine(ConnectionStatusLoop());
            
            // Auto countdown timer
            StartCoroutine(CountdownLoop());
        }

        private void OnDestroy()
        {
            if (RealtimeLeaderboardService.Instance != null)
                RealtimeLeaderboardService.Instance.LeaderboardUpdated -= OnLeaderboardUpdated;

            if (EventManager.Instance != null)
            {
                EventManager.Instance.OnEventUpdated -= OnEventUpdated;
                EventManager.Instance.OnEventEnded   -= OnEventEnded;
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void OpenPanel()
        {
            if (panelRoot  != null) panelRoot.SetActive(true);
            if (openButton != null) openButton.gameObject.SetActive(false);

            // If the event is completed, data is frozen — fetch once via REST, no WebSocket needed.
            // If the event is active/upcoming, the WebSocket already has live data in latestData.
            var em = EventManager.Instance;
            bool isCompleted = em != null && em.CurrentEvent != null 
                            && em.CurrentEvent.status == "completed";

            if (isCompleted && latestData == null)
            {
                // One-shot REST fetch for final results
                var apiService = TycoonAPIService.Instance;
                if (apiService != null)
                {
                    if (connectionStatusLabel != null)
                    {
                        connectionStatusLabel.text  = "● LOADING";
                        connectionStatusLabel.color = new Color(1f, 0.65f, 0.1f, 1f);
                    }
                    apiService.GetEventLeaderboard(
                        (data) =>
                        {
                            latestData = data;
                            RefreshRows();
                            if (connectionStatusLabel != null)
                            {
                                connectionStatusLabel.text  = "● FINAL RESULTS";
                                connectionStatusLabel.color = new Color(0.27f, 0.90f, 0.45f, 1f);
                            }
                        },
                        (err) => Debug.LogWarning($"[LeaderboardPanel] Failed to fetch final leaderboard: {err}")
                    );
                }
            }

            RefreshRows();
        }

        public void ClosePanel()
        {
            if (panelRoot  != null) panelRoot.SetActive(false);
            if (openButton != null) openButton.gameObject.SetActive(true);
        }

        // ── Live update handler ───────────────────────────────────────────────

        private void OnLeaderboardUpdated(LeaderboardUpdateEvent update)
        {
            latestData = update;

            if (panelRoot != null && panelRoot.activeSelf)
                RefreshRows();
                
            if (titleLabel != null && !string.IsNullOrEmpty(update.franchise_name))
            {
                titleLabel.text = $"🏆 {update.franchise_name.ToUpper()} TOURNAMENT";
            }
        }
        
        // ── EventManager handlers ─────────────────────────────────────────────

        private void OnEventUpdated(EventResponse eventData)
        {
            // An event exists — enable the button
            RefreshOpenButton();
        }

        private void OnEventEnded()
        {
            // No more event — disable the button and close panel if open
            RefreshOpenButton();
            if (panelRoot != null && panelRoot.activeSelf)
                ClosePanel();
        }

        /// <summary>
        /// Enables or disables the leaderboard open-button based on
        /// whether EventManager currently has an active/upcoming event.
        /// </summary>
        private void RefreshOpenButton()
        {
            if (openButton == null) return;

            bool hasEvent = EventManager.Instance != null
                         && EventManager.Instance.CurrentEvent != null;

            openButton.interactable = hasEvent;

            // Dim button alpha when disabled so it looks visually inactive
            var graphic = openButton.targetGraphic;
            if (graphic != null)
            {
                Color c = graphic.color;
                c.a = hasEvent ? 1f : 0.4f;
                graphic.color = c;
            }

            // Update button label if one is assigned
            if (openButtonText != null)
                openButtonText.text = hasEvent ? "Leaderboard" : "No Event";
        }
        
        private IEnumerator CountdownLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                if (latestData != null && latestData.time_remaining_seconds > 0)
                {
                    latestData.time_remaining_seconds--;
                    if (lastUpdatedLabel != null && panelRoot != null && panelRoot.activeSelf)
                    {
                        int min = latestData.time_remaining_seconds / 60;
                        int sec = latestData.time_remaining_seconds % 60;
                        lastUpdatedLabel.text = string.Format("Ends in {0:D2}:{1:D2}", min, sec);
                    }
                }
                else if (latestData != null && latestData.time_remaining_seconds <= 0)
                {
                    if (lastUpdatedLabel != null && panelRoot != null && panelRoot.activeSelf)
                    {
                        lastUpdatedLabel.text = "EVENT ENDED";
                    }
                }
            }
        }

        // ── Row rendering ─────────────────────────────────────────────────────

        private void RefreshRows()
        {
            foreach (var r in spawnedRows)
                if (r != null) Destroy(r);
            spawnedRows.Clear();

            bool hasData = latestData != null && latestData.entries != null && latestData.entries.Count > 0;

            if (!hasData)
            {
                if (emptyLabel != null)
                {
                    emptyLabel.gameObject.SetActive(true);
                    
                    var em = EventManager.Instance;
                    bool isCompleted = em != null && em.CurrentEvent != null && em.CurrentEvent.status == "completed";
                    
                    if (isCompleted)
                    {
                        emptyLabel.text = "Tournament finished with no participants.";
                    }
                    else
                    {
                        bool live = RealtimeLeaderboardService.Instance != null
                                 && RealtimeLeaderboardService.Instance.IsConnected;
                        emptyLabel.text = live
                            ? "No active tournament right now."
                            : "Connecting to leaderboard server...";
                    }
                }
                return;
            }

            if (emptyLabel != null) emptyLabel.gameObject.SetActive(false);

            foreach (var entry in latestData.entries)
            {
                if (rowPrefab == null || rowContainer == null) break;

                GameObject row = Instantiate(rowPrefab, rowContainer, false);
                row.SetActive(true);
                spawnedRows.Add(row);

                Color  nameColor;
                string rankText;

                if (entry.rank == 1)      { nameColor = ColGold;    rankText = "#1"; }
                else if (entry.rank == 2) { nameColor = ColSilver;  rankText = "#2"; }
                else if (entry.rank == 3) { nameColor = ColBronze;  rankText = "#3"; }
                else                      { nameColor = ColDefault; rankText = "#" + entry.rank; }

                string valueText = string.Format("Rs.{0:N0}", entry.value);

                ApplyRowData(row, rankText, entry.display_name, valueText, nameColor, entry.rank);
            }
        }

        private static void ApplyRowData(
            GameObject row,
            string rank, string playerName, string value,
            Color nameColor, int rowIndex)
        {
            var labels = row.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (labels.Length >= 3)
            {
                labels[0].text  = rank;
                labels[1].text  = playerName;
                labels[1].color = nameColor;
                labels[2].text  = value;
            }

            var bg = row.GetComponent<Image>();
            if (bg != null)
                bg.color = rowIndex % 2 == 0
                    ? new Color(1f, 1f, 1f, 0.04f)
                    : new Color(1f, 1f, 1f, 0.00f);
        }

        // ── Connection status ─────────────────────────────────────────────────

        private void RefreshConnectionStatus()
        {
            if (connectionStatusLabel == null) return;

            // Do not overwrite the connection status if the event is already completed.
            // The REST fetch handles displaying "● FINAL RESULTS" or "● LOADING".
            var em = EventManager.Instance;
            if (em != null && em.CurrentEvent != null && em.CurrentEvent.status == "completed")
                return;

            bool connected = RealtimeLeaderboardService.Instance != null
                          && RealtimeLeaderboardService.Instance.IsConnected;

            connectionStatusLabel.text  = connected ? "● LIVE" : "● CONNECTING";
            connectionStatusLabel.color = connected ? ColLive : ColConnecting;
        }

        private IEnumerator ConnectionStatusLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(3f);
                RefreshConnectionStatus();
            }
        }
    }
}
