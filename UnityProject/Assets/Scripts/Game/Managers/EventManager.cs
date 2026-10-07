using System;
using UnityEngine;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Manages franchise tournament events.
    /// After registration the player places the event store on their own grid
    /// (same flow as a normal building, but free). On win the store becomes
    /// permanent exactly where placed; on loss it is removed by the backend.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }

        [Header("Polling Settings")]
        [SerializeField] private float pollInterval = 60f;
        [SerializeField] private float initialPollDelay = 2f;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        public EventResponse CurrentEvent { get; private set; }

        public event Action<EventResponse> OnEventUpdated;
        public event Action OnEventEnded;
        public event Action<string> OnRegistrationSuccess;
        public event Action<string> OnRegistrationFailed;

        private float pollTimer;
        private bool isPolling;
        private bool isFirstPoll = true;

        // Pending placement state — set after registration, consumed by placement callback
        private string pendingEventId;
        private string pendingPlayerId;
        private Action<bool> pendingCallback;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (enableDebugLogs) Debug.Log("[EventManager] Initialized");
        }

        private void Start()
        {
            pollTimer = initialPollDelay;
            isPolling = true;
        }

        private void Update()
        {
            if (!isPolling) return;
            pollTimer -= Time.deltaTime;
            if (pollTimer <= 0f)
            {
                FetchActiveEvent();
                pollTimer = isFirstPoll ? pollInterval / 2f : pollInterval;
                isFirstPoll = false;
            }
        }

        public void FetchActiveEvent()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentPlayer == null) return;
            string playerId = gm.CurrentPlayer.player_id;
            if (enableDebugLogs) Debug.Log($"[EventManager] Fetching event for {playerId}");
            TycoonAPIService.Instance.GetActiveEvent(playerId, OnEventFetchSuccess, OnEventFetchError);
        }

        public void RegisterForEvent(Action<bool> callback = null)
        {
            if (CurrentEvent == null)
            {
                Debug.LogWarning("[EventManager] No active event");
                OnRegistrationFailed?.Invoke("No active event");
                callback?.Invoke(false);
                return;
            }
            if (CurrentEvent.is_registered)
            {
                OnRegistrationFailed?.Invoke("Already registered");
                callback?.Invoke(false);
                return;
            }
            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentPlayer == null)
            {
                OnRegistrationFailed?.Invoke("Player data not available");
                callback?.Invoke(false);
                return;
            }
            float playerMoney = gm.CurrentPlayer.money;
            if (playerMoney < CurrentEvent.entry_fee)
            {
                OnRegistrationFailed?.Invoke($"Need ₹{CurrentEvent.entry_fee:N0}, have ₹{playerMoney:N0}");
                callback?.Invoke(false);
                return;
            }
            TycoonAPIService.Instance.RegisterForEvent(
                CurrentEvent.event_id,
                gm.CurrentPlayer.player_id,
                (r) => OnRegSuccess(r, callback),
                (e) => OnRegError(e, callback)
            );
        }

        /// <summary>
        /// Allows the player to re-enter placement mode if they previously
        /// cancelled it (e.g. they needed to go buy empty land first).
        /// </summary>
        public void RetryEventPlacement(Action<bool> callback = null)
        {
            if (CurrentEvent == null || !CurrentEvent.is_registered)
            {
                Debug.LogWarning("[EventManager] Cannot retry placement: Not registered for an active event.");
                callback?.Invoke(false);
                return;
            }

            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentPlayer == null)
            {
                callback?.Invoke(false);
                return;
            }

            // Make sure they don't already have the store placed!
            foreach (var biz in gm.CurrentPlayer.businesses)
            {
                if (biz.is_event_business && biz.event_id == CurrentEvent.event_id)
                {
                    Debug.LogWarning("[EventManager] Event store is already placed!");
                    callback?.Invoke(false);
                    return;
                }
            }

            var bpm = BuildingPlacementManager.Instance;
            if (bpm == null)
            {
                Debug.LogWarning("[EventManager] BuildingPlacementManager not found.");
                callback?.Invoke(false);
                return;
            }

            pendingEventId  = CurrentEvent.event_id;
            pendingPlayerId = gm.CurrentPlayer.player_id;
            pendingCallback = callback;

            bpm.OnPlacementCompleted += OnEventStorePlaced;
            bpm.OnPlacementCancelled += OnEventStorePlacementCancelled;
            bpm.StartEventPlacement(CurrentEvent.franchise_name, gm.GetBuildingPrefabForEvent(CurrentEvent.franchise_name));

            if (enableDebugLogs) Debug.Log("[EventManager] Retrying placement mode for event store.");
        }

        public void StopPolling()  { isPolling = false; }
        public void StartPolling() { isPolling = true; pollTimer = 1f; }
        
        // ── Private Callbacks ──────────────────────────────────────────────

        private void OnEventFetchSuccess(EventResponse response)
        {
            if (response == null)
            {
                if (CurrentEvent != null)
                {
                    GameManager.Instance?.DespawnEventBusinesses(CurrentEvent.event_id);
                    CurrentEvent = null;
                    OnEventEnded?.Invoke();
                }
                else
                {
                    GameManager.Instance?.DespawnEventBusinesses(null);
                }
                return;
            }

            bool isNew      = CurrentEvent == null || CurrentEvent.event_id != response.event_id;
            bool regChanged = CurrentEvent != null  && CurrentEvent.is_registered != response.is_registered;
            CurrentEvent = response;

            if (isNew || regChanged) OnEventUpdated?.Invoke(response);

            if (isNew)
            {
                GameManager.Instance?.DespawnEventBusinesses(response.event_id, true);

                if (CurrentEvent.is_registered)
                {
                    var gm = GameManager.Instance;
                    if (gm != null && gm.CurrentPlayer != null)
                    {
                        foreach (var biz in gm.CurrentPlayer.businesses)
                        {
                            if (biz.is_event_business && biz.event_id == CurrentEvent.event_id)
                            {
                                if (!gm.IsBusinessSpawned(biz.business_id))
                                {
                                    if (enableDebugLogs)
                                        Debug.Log($"[EventManager] Re-spawning event business '{biz.name}' at saved grid pos ({biz.position_x},{biz.position_y})");
                                    gm.OnBusinessCreatedCallback(biz);
                                }
                                break;
                            }
                        }
                    }
                }
            }
        }

        private void OnEventFetchError(string error)
        {
            if (enableDebugLogs) Debug.LogWarning($"[EventManager] Fetch error: {error}");
        }

        private void OnRegSuccess(EventResponse r, Action<bool> callback)
        {
            if (enableDebugLogs) Debug.Log($"[EventManager] Registered for {r.franchise_name}");
            CurrentEvent = r;
            OnRegistrationSuccess?.Invoke($"Registered for {r.franchise_name}! Now place your store on an owned tile.");
            OnEventUpdated?.Invoke(r);

            var gm = GameManager.Instance;
            if (gm == null || gm.CurrentPlayer == null) { callback?.Invoke(true); return; }

            foreach (var biz in gm.CurrentPlayer.businesses)
            {
                if (biz.is_event_business && biz.event_id == r.event_id)
                {
                    if (enableDebugLogs) Debug.Log("[EventManager] Event business already placed — spawning at saved position.");
                    gm.OnBusinessCreatedCallback(biz);
                    callback?.Invoke(true);
                    return;
                }
            }

            var bpm = BuildingPlacementManager.Instance;
            if (bpm == null)
            {
                Debug.LogWarning("[EventManager] BuildingPlacementManager not found — skipping placement mode.");
                callback?.Invoke(true);
                return;
            }

            pendingEventId  = r.event_id;
            pendingPlayerId = gm.CurrentPlayer.player_id;
            pendingCallback = callback;

            bpm.OnPlacementCompleted += OnEventStorePlaced;
            bpm.OnPlacementCancelled += OnEventStorePlacementCancelled;
            bpm.StartEventPlacement(r.franchise_name, gm.GetBuildingPrefabForEvent(r.franchise_name));

            if (enableDebugLogs) Debug.Log("[EventManager] Entered placement mode for event store.");
        }

        /// <summary>
        /// Fired by BuildingPlacementManager when the player clicks a valid owned tile.
        /// Sends the chosen coordinates to the backend to create the event business there.
        /// </summary>
        private void OnEventStorePlaced(BusinessData placedBusiness)
        {
            var bpm = BuildingPlacementManager.Instance;
            if (bpm != null)
            {
                bpm.OnPlacementCompleted -= OnEventStorePlaced;
                bpm.OnPlacementCancelled -= OnEventStorePlacementCancelled;
            }

            if (string.IsNullOrEmpty(pendingEventId)) return;

            string eventId  = pendingEventId;
            string playerId = pendingPlayerId;
            var    cb       = pendingCallback;
            pendingEventId  = null;
            pendingPlayerId = null;
            pendingCallback = null;

            if (enableDebugLogs)
                Debug.Log($"[EventManager] Player chose ({placedBusiness.position_x},{placedBusiness.position_y}) — calling backend.");

            TycoonAPIService.Instance.CreateEventBusiness(
                eventId,
                playerId,
                placedBusiness.position_x,
                placedBusiness.position_y,
                (business) =>
                {
                    if (enableDebugLogs)
                        Debug.Log($"[EventManager] Event business confirmed at ({business.position_x},{business.position_y})");
                    // Use the normal grid-spawn path so the tile is marked as occupied
                    GameManager.Instance?.OnBusinessCreatedCallback(business);
                    cb?.Invoke(true);
                },
                (error) =>
                {
                    Debug.LogWarning($"[EventManager] Backend rejected placement: {error}");
                    GameManager.Instance?.RefreshPlayerData();
                    cb?.Invoke(false);
                }
            );
        }

        private void OnEventStorePlacementCancelled()
        {
            var bpm = BuildingPlacementManager.Instance;
            if (bpm != null)
            {
                bpm.OnPlacementCompleted -= OnEventStorePlaced;
                bpm.OnPlacementCancelled -= OnEventStorePlacementCancelled;
            }

            if (enableDebugLogs) Debug.Log("[EventManager] Event store placement cancelled.");
            // Registration already succeeded — player can place the store later
            pendingCallback?.Invoke(true);
            pendingEventId  = null;
            pendingPlayerId = null;
            pendingCallback = null;
        }

        private void OnRegError(string error, Action<bool> callback)
        {
            Debug.LogError($"[EventManager] Registration failed: {error}");
            OnRegistrationFailed?.Invoke($"Failed: {error}");
            callback?.Invoke(false);
        }
    }
}
