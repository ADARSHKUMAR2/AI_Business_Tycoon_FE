using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using AIBusinessTycoon.Config;

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

            cancellationSource = new CancellationTokenSource();
            connectionTask = ConnectLoopAsync(cancellationSource.Token);
        }

        private async Task ConnectLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    socket = new ClientWebSocket();
                    await socket.ConnectAsync(new Uri(GetWebSocketUrl()), token);
                    Debug.Log("[RealtimeLeaderboardService] Connected to leaderboard event stream.");

                    await SendSubscriptionAsync(token);
                    await ReceiveMessagesAsync(token);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception error)
                {
                    Debug.LogWarning($"[RealtimeLeaderboardService] Connection failed: {error.Message}");
                }
                finally
                {
                    socket?.Dispose();
                    socket = null;
                }

                if (!token.IsCancellationRequested)
                    await Task.Delay(TimeSpan.FromSeconds(reconnectDelaySeconds), token);
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
            return $"{scheme}://{backendUri.Host}:{gameServicePort}/events/leaderboard";
        }

        private async Task SendSubscriptionAsync(CancellationToken token)
        {
            string message = JsonConvert.SerializeObject(new RealtimeSubscribeMessage());
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
                    break;

                string json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                LeaderboardUpdateEvent update = JsonConvert.DeserializeObject<LeaderboardUpdateEvent>(json);
                if (update != null && update.type == "leaderboard.updated")
                {
                    // ClientWebSocket completes on a worker thread. Queue data so
                    // UI subscribers are invoked from Unity's main thread in Update.
                    lock (pendingUpdatesLock)
                        pendingUpdates.Enqueue(update);
                }
            }
        }

        private void Update()
        {
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

        private async void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            if (cancellationSource == null)
                return;

            cancellationSource.Cancel();
            try
            {
                if (socket != null && socket.State == WebSocketState.Open)
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Application closed", CancellationToken.None);
            }
            catch (Exception)
            {
                // Cancellation/disposal is expected while the Editor stops Play Mode.
            }
            finally
            {
                cancellationSource.Dispose();
            }
        }
    }
}