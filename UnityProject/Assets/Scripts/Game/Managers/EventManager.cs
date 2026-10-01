using System;
using UnityEngine;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Services;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Manages franchise tournament events.
    /// Polls the backend for active events and notifies UI components.
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
                string msg = $"Need ₹{CurrentEvent.entry_fee:N0}, have ₹{playerMoney:N0}";
                OnRegistrationFailed?.Invoke(msg);
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
        
        private void OnEventFetchSuccess(EventResponse response)
        {
            if (response == null)
            {
                if (CurrentEvent != null)
                {
                    CurrentEvent = null;
                    OnEventEnded?.Invoke();
                }
                return;
            }
            
            bool isNew = CurrentEvent == null || CurrentEvent.event_id != response.event_id;
            bool regChanged = CurrentEvent != null && CurrentEvent.is_registered != response.is_registered;
            CurrentEvent = response;
            
            if (isNew || regChanged) OnEventUpdated?.Invoke(response);
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
            
            // Automatically create event business
            var gm = GameManager.Instance;
            if (gm != null && gm.CurrentPlayer != null)
            {
                if (enableDebugLogs) Debug.Log($"[EventManager] Creating event business for {r.franchise_name}");
                TycoonAPIService.Instance.CreateEventBusiness(
                    r.event_id,
                    gm.CurrentPlayer.player_id,
                    (business) => OnEventBusinessCreated(business, callback),
                    (error) => OnEventBusinessError(error, callback)
                );
            }
            else
            {
                callback?.Invoke(true);
            }
        }
        
        private void OnEventBusinessCreated(BusinessData business, Action<bool> callback)
        {
            if (enableDebugLogs) Debug.Log($"[EventManager] Event business created: {business.name} at ({business.position_x}, {business.position_y})");
            // Reload player data to get the new business
            // GameManager will handle this when player data is refreshed
            callback?.Invoke(true);
        }
        
        private void OnEventBusinessError(string error, Action<bool> callback)
        {
            Debug.LogWarning($"[EventManager] Failed to create event business: {error}");
            // Still mark registration as success since the player is registered
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
