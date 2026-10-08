using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using AIBusinessTycoon.Config;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.Authentication
{
    /// <summary>
    /// Handles all communication with the Auth Service (Port 8001).
    /// Saves player_id to PlayerPrefs after successful login/register.
    /// </summary>
    public class TycoonAuthService : MonoBehaviour
    {
        public static TycoonAuthService Instance { get; private set; }

        public const string PLAYER_ID_KEY  = "tycoon_player_id";
        public const string PLAYER_NAME_KEY = "tycoon_player_name";
        public const string IS_GUEST_KEY    = "tycoon_is_guest";

        [Header("Configuration")]
        [SerializeField] private BackendConfig backendConfig;

        private string AuthBaseUrl => backendConfig != null
            ? backendConfig.GetActiveURL() + "/api/auth"
            : "http://localhost:8000/api/auth";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public static bool HasSavedSession()
        {
            return PlayerPrefs.HasKey(PLAYER_ID_KEY) &&
                   !string.IsNullOrEmpty(PlayerPrefs.GetString(PLAYER_ID_KEY));
        }

        public static string GetSavedPlayerId()
            => PlayerPrefs.GetString(PLAYER_ID_KEY, string.Empty);

        public static string GetSavedPlayerName()
            => PlayerPrefs.GetString(PLAYER_NAME_KEY, "Tycoon");

        public static void ClearSession()
        {
            PlayerPrefs.DeleteKey(PLAYER_ID_KEY);
            PlayerPrefs.DeleteKey(PLAYER_NAME_KEY);
            PlayerPrefs.DeleteKey(IS_GUEST_KEY);
            PlayerPrefs.Save();
        }

        public void Register(string playerName, string email, string password,
                             Action<AuthResponse> onSuccess, Action<string> onError)
        {
            var payload = new RegisterRequest { name = playerName, email = email, password = password };
            StartCoroutine(Post<RegisterRequest, AuthResponse>($"{AuthBaseUrl}/register", payload, onSuccess, onError));
        }

        public void Login(string email, string password,
                          Action<AuthResponse> onSuccess, Action<string> onError)
        {
            var payload = new LoginRequest { email = email, password = password };
            StartCoroutine(Post<LoginRequest, AuthResponse>($"{AuthBaseUrl}/login", payload, onSuccess, onError));
        }

        public void LoginGuest(string guestName,
                               Action<AuthResponse> onSuccess, Action<string> onError)
        {
            var payload = new GuestLoginRequest { name = guestName };
            StartCoroutine(Post<GuestLoginRequest, AuthResponse>($"{AuthBaseUrl}/guest", payload, onSuccess, onError));
        }

        // ── Session Persistence ───────────────────────────────────────────────

        public static void SaveSession(AuthResponse response)
        {
            PlayerPrefs.SetString(PLAYER_ID_KEY,   response.player_id);
            PlayerPrefs.SetString(PLAYER_NAME_KEY, response.name);
            PlayerPrefs.SetInt(IS_GUEST_KEY,       response.is_guest ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[TycoonAuthService] Session saved for '{response.name}' ({response.player_id})");
        }

        // ── HTTP Helper ───────────────────────────────────────────────────────

        private IEnumerator Post<TReq, TRes>(string url, TReq payload,
                                             Action<TRes> onSuccess, Action<string> onError)
        {
            string json = JsonConvert.SerializeObject(payload);
            byte[] body = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler   = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.timeout = 10;

                yield return req.SendWebRequest();

                if (req.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        TRes data = JsonConvert.DeserializeObject<TRes>(req.downloadHandler.text);
                        onSuccess?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        onError?.Invoke($"Failed to parse response: {e.Message}");
                    }
                }
                else
                {
                    // Try to extract backend error message
                    try
                    {
                        var err = JsonConvert.DeserializeObject<BackendErrorWrapper>(req.downloadHandler.text);
                        onError?.Invoke(err?.error?.message ?? req.error);
                    }
                    catch
                    {
                        onError?.Invoke(req.error);
                    }
                }
            }
        }

        // Helper class to deserialize backend error shape
        [Serializable]
        private class BackendErrorWrapper
        {
            public BackendErrorBody error;
        }

        [Serializable]
        private class BackendErrorBody
        {
            public string message;
        }
    }
}
