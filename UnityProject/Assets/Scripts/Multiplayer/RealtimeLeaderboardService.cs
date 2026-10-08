using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using AIBusinessTycoon.Config;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.Multiplayer
{
    /// <summary>
    /// Receives display-only competitive leaderboard events. Gameplay movement,
    /// customer simulation, inventory, and transactions remain local/REST-driven.
    /// </summary>
    public class RealtimeLeaderboardService : MonoBehaviour
    {
        public static RealtimeLeaderboardService Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private BackendConfig backendConfig;
        [SerializeField, Min(1f)] private float reconnectDelaySeconds = 5f;

        private ClientWebSocket socket;
        private CancellationTokenSource cancellationSource;
        private Task connectionTask;
        private readonly Queue<LeaderboardUpdateEvent> pendingUpdates = new Queue<LeaderboardUpdateEvent>();
        private readonly object pendingUpdatesLock = new object();

        public event Action<LeaderboardUpdateEvent> LeaderboardUpdated;

        [Header("Debug")]
        [SerializeField, Tooltip("Read-only. Shows current WebSocket state.")]
        private string currentSocketState = "Disconnected";

        public bool IsConnected => socket != null && socket.State == WebSocketState.Open;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (backendConfig == null)
            {
                Debug.LogWarning("[RealtimeLeaderboardService] BackendConfig is not assigned; realtime leaderboard is disabled.");
                return;
            }

            // Do NOT connect on startup. We wait for EventManager to tell us we are registered.
            if (AIBusinessTycoon.Managers.EventManager.Instance != null)
            {
                AIBusinessTycoon.Managers.EventManager.Instance.OnEventUpdated += HandleEventUpdate;
                AIBusinessTycoon.Managers.EventManager.Instance.OnEventEnded += HandleEventEnded;
            }
        }

        private void HandleEventUpdate(EventResponse eventData)
        {
            bool shouldConnect = eventData != null && 
                               eventData.status == "active" && 
                               eventData.is_registered;

            if (shouldConnect && cancellationSource == null)
            {
                Debug.Log("[RealtimeLeaderboardService] Player is registered in an active event. Starting WebSocket...");
                cancellationSource = new CancellationTokenSource();
                connectionTask = ConnectLoopAsync(cancellationSource.Token);
            }
            else if (!shouldConnect && cancellationSource != null)
            {
                Debug.Log("[RealtimeLeaderboardService] Player not registered or event ended. Stopping WebSocket...");
                StopConnection();
            }
        }

        private void HandleEventEnded()
        {
            if (cancellationSource != null)
            {
                Debug.Log("[RealtimeLeaderboardService] Event ended completely. Stopping WebSocket...");
                StopConnection();
            }
        }

        private async void StopConnection()
        {
            if (cancellationSource == null) return;
            
            cancellationSource.Cancel();
            try
            {
                if (socket != null && socket.State == WebSocketState.Open)
                {
                    Debug.Log("[RealtimeLeaderboardService] Disconnecting WebSocket gracefully...");
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Event ended or unregistered", CancellationToken.None);
                    Debug.Log("[RealtimeLeaderboardService] Disconnected.");
                }
            }
            catch (Exception) { /* expected during shutdown */ }
            finally
            {
                cancellationSource.Dispose();
                cancellationSource = null;
            }
        }

        private async Task ConnectLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    socket = new ClientWebSocket();
                    Debug.Log($"[RealtimeLeaderboardService] Connecting to WebSocket at {GetWebSocketUrl()}...");
                    await socket.ConnectAsync(new Uri(GetWebSocketUrl()), token);
                    Debug.Log("[RealtimeLeaderboardService] Connected to leaderboard event stream successfully!");

                    await SendSubscriptionAsync(token);
                    await ReceiveMessagesAsync(token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    Debug.Log("[RealtimeLeaderboardService] WebSocket connection cancelled by application.");
                    break;
                }
                catch (Exception error)
                {
                    Debug.LogWarning($"[RealtimeLeaderboardService] Connection failed: {error.Message}");
                }
                finally
                {
                    if (socket != null)
                    {
                        socket.Dispose();
                        socket = null;
                    }
                }

                if (!token.IsCancellationRequested)
                {
                    Debug.LogWarning($"[RealtimeLeaderboardService] Disconnected. Attempting to reconnect in {reconnectDelaySeconds} seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds), token);
                }
            }
        }

        private string GetWebSocketUrl()
        {
            string baseUrl = backendConfig.GetActiveURL().TrimEnd('/');
            string scheme = baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
            Uri backendUri = new Uri(baseUrl);

            // The current HTTP gateway only proxies REST. During development this
            // intentionally connects directly to the Game Service on port 8002.
            int gameServicePort = backendUri.IsLoopback ? 8002 : backendUri.Port;
            string host = backendUri.IsLoopback ? "127.0.0.1" : backendUri.Host;
            return $"{scheme}://{host}:{gameServicePort}/events/leaderboard";
        }

        private async Task SendSubscriptionAsync(CancellationToken token)
        {
            string message = JsonConvert.SerializeObject(new RealtimeSubscribeMessage());
            Debug.Log($"[RealtimeLeaderboardService] Sending to server: {message}");
            byte[] bytes = Encoding.UTF8.GetBytes(message);
            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);
        }

        private async Task ReceiveMessagesAsync(CancellationToken token)
        {
            byte[] buffer = new byte[8192];
            while (socket != null && socket.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                WebSocketReceiveResult result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log("[RealtimeLeaderboardService] Server closed connection.");
                    break;
                }

                string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Debug.Log($"[RealtimeLeaderboardService] Received raw: {json}");

                try 
                {
                    LeaderboardUpdateEvent update = JsonConvert.DeserializeObject<LeaderboardUpdateEvent>(json);
                    
                    if (update != null && update.type == "leaderboard.updated")
                    {
                        lock (pendingUpdatesLock)
                            pendingUpdates.Enqueue(update);
                    }
                    else if (json.Contains("\"error\""))
                    {
                        Debug.LogError($"[RealtimeLeaderboardService] Server sent error: {json}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[RealtimeLeaderboardService] Failed to parse WebSocket JSON: {e.Message}\\nJSON: {json}");
                }
            }
        }

        private void Update()
        {
            // Keep Inspector updated with current socket state
            if (socket == null)
                currentSocketState = "Disconnected";
            else
                currentSocketState = socket.State.ToString();

            while (true)
            {
                LeaderboardUpdateEvent update;
                lock (pendingUpdatesLock)
                {
                    if (pendingUpdates.Count == 0)
                        return;
                    update = pendingUpdates.Dequeue();
                }

                LeaderboardUpdated?.Invoke(update);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (AIBusinessTycoon.Managers.EventManager.Instance != null)
            {
                AIBusinessTycoon.Managers.EventManager.Instance.OnEventUpdated -= HandleEventUpdate;
                AIBusinessTycoon.Managers.EventManager.Instance.OnEventEnded -= HandleEventEnded;
            }

            StopConnection();
        }
    }
}