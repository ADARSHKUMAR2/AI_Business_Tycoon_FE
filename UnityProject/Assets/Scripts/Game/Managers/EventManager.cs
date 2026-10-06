using System;
using UnityEngine;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Manages franchise tournament events.
    /// Polls the backend for active events, spawns/despawns the temporary
    /// event business prefab, and notifies UI components.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        public static EventManager Instance { get; private set; }
        
        /// <summary>
        /// Fixed world-space position where event businesses are spawned.
        /// Placed well outside the normal player grid so it never conflicts.
        /// </summary>
        public static readonly Vector3 EventZoneWorldPosition = new Vector3(30f, 0f, 0f);
        
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
        
        public void StopPolling() { isPolling = false; }
        public void StartPolling() { isPolling = true; pollTimer = 1f; }
        
        // ── Private Callbacks ──────────────────────────────────────────────
        
        private void OnEventFetchSuccess(EventResponse response)
        {
            if (response == null)
            {
                if (CurrentEvent != null)
                {
                    // Event ended — despawn the temporary store
                    GameManager.Instance?.DespawnEventBusinesses(CurrentEvent.event_id);
                    CurrentEvent = null;
                    OnEventEnded?.Invoke();
                }
                else
                {
                    // No active event on startup. Ensure no ghost event businesses are spawned.
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
                // Cleanup any ghost event businesses that don't match the current active event
                GameManager.Instance?.DespawnEventBusinesses(response.event_id, true);
                
                // If the player loaded into the game while already registered for this active event,
                // we need to spawn their event business prefab now.
                if (CurrentEvent.is_registered)
                {
                    var gm = GameManager.Instance;
                    if (gm != null && gm.CurrentPlayer != null)
                    {
                        foreach (var biz in gm.CurrentPlayer.businesses)
                        {
                            if (biz.is_event_business && biz.event_id == CurrentEvent.event_id)
                            {
                                if (enableDebugLogs) Debug.Log($"[EventManager] Spawning existing event business for active event: {biz.name}");
                                gm.SpawnEventBusiness(biz, EventZoneWorldPosition);
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
            OnRegistrationSuccess?.Invoke($"Registered for {r.franchise_name}!");
            OnEventUpdated?.Invoke(r);
            
            // Create event business on backend, then spawn prefab
            var gm = GameManager.Instance;
            if (gm != null && gm.CurrentPlayer != null)
            {
                if (enableDebugLogs) Debug.Log($"[EventManager] Creating event business for {r.franchise_name}");
                TycoonAPIService.Instance.CreateEventBusiness(
                    r.event_id,
                    gm.CurrentPlayer.player_id,
                    (business) => OnEventBusinessCreated(business, callback),
                    (error)    => OnEventBusinessError(error, callback)
                );
            }
            else
            {
                callback?.Invoke(true);
            }
        }
        
        private void OnEventBusinessCreated(BusinessData business, Action<bool> callback)
        {
            if (enableDebugLogs)
                Debug.Log($"[EventManager] Backend business created: {business.name} — spawning prefab at {EventZoneWorldPosition}");
            
            var gm = GameManager.Instance;
            if (gm != null)
            {
                GameObject spawned = gm.SpawnEventBusiness(business, EventZoneWorldPosition);
                if (spawned == null)
                    Debug.LogWarning("[EventManager] SpawnEventBusiness returned null — check prefab assignments in GameManager.");
            }
            
            callback?.Invoke(true);
        }
        
        private void OnEventBusinessError(string error, Action<bool> callback)
        {
            Debug.LogWarning($"[EventManager] Failed to create event business: {error}");
            // Registration succeeded — let the player proceed
            callback?.Invoke(true);
        }
        
        private void OnRegError(string error, Action<bool> callback)
        {
            Debug.LogError($"[EventManager] Registration failed: {error}");
            OnRegistrationFailed?.Invoke($"Failed: {error}");
            callback?.Invoke(false);
        }
    }
}
