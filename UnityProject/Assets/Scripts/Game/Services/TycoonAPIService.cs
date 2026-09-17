using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using AIBusinessTycoon.Config;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.Services
{
    /// <summary>
    /// Handles all HTTP communication with the FastAPI backend.
    /// Uses UnityWebRequest for async operations.
    /// </summary>
    public class TycoonAPIService : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BackendConfig backendConfig;
        
        [Header("Settings")]
        [SerializeField] private int requestTimeout = 10; // seconds
        
        private static TycoonAPIService _instance;
        public static TycoonAPIService Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("TycoonAPIService");
                    _instance = go.AddComponent<TycoonAPIService>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Fetches player data from the backend.
        /// </summary>
        public void GetPlayerData(string playerId, Action<PlayerTycoonData> onSuccess, Action<string> onError)
        {
            if (backendConfig == null)
            {
                onError?.Invoke("BackendConfig is not assigned!");
                return;
            }
            
            StartCoroutine(GetPlayerDataCoroutine(playerId, onSuccess, onError));
        }
        
        private IEnumerator GetPlayerDataCoroutine(string playerId, Action<PlayerTycoonData> onSuccess, Action<string> onError)
        {
            string url = backendConfig.GetPlayerDataURL(playerId);
            Debug.Log($"[TycoonAPIService] Requesting player data from: {url}");
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = requestTimeout;
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonResponse = request.downloadHandler.text;
                        Debug.Log($"[TycoonAPIService] Raw response: {jsonResponse}");
                        
                        // Backend returns player data directly (not wrapped in success/message)
                        PlayerTycoonData playerData = JsonUtility.FromJson<PlayerTycoonData>(jsonResponse);
                        
                        if (playerData != null && !string.IsNullOrEmpty(playerData.player_id))
                        {
                            Debug.Log($"[TycoonAPIService] Player data received: {playerData}");
                            onSuccess?.Invoke(playerData);
                        }
                        else
                        {
                            string errorMsg = "Failed to parse player data";
                            Debug.LogError($"[TycoonAPIService] {errorMsg}");
                            onError?.Invoke(errorMsg);
                        }
                    }
                    catch (Exception e)
                    {
                        string errorMsg = $"Failed to parse JSON response: {e.Message}";
                        Debug.LogError($"[TycoonAPIService] {errorMsg}");
                        onError?.Invoke(errorMsg);
                    }
                }
                else
                {
                    string errorMsg = $"Request failed: {request.error} (Code: {request.responseCode})";
                    Debug.LogError($"[TycoonAPIService] {errorMsg}");
                    onError?.Invoke(errorMsg);
                }
            }
        }

        
        /// <summary>
        /// Updates player data on the backend.
        /// </summary>
        public void UpdatePlayerData(PlayerTycoonData data, Action<bool> onComplete)
        {
            if (backendConfig == null)
            {
                Debug.LogError("[TycoonAPIService] BackendConfig is not assigned!");
                onComplete?.Invoke(false);
                return;
            }
            
            StartCoroutine(UpdatePlayerDataCoroutine(data, onComplete));
        }
        
        private IEnumerator UpdatePlayerDataCoroutine(PlayerTycoonData data, Action<bool> onComplete)
        {
            string url = backendConfig.GetUpdatePlayerDataURL();
            Debug.Log($"[TycoonAPIService] Updating player data at: {url}");
            
            string jsonData = JsonUtility.ToJson(data);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = requestTimeout;
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[TycoonAPIService] Player data updated successfully");
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[TycoonAPIService] Update failed: {request.error}");
                    onComplete?.Invoke(false);
                }
            }
        }
    }
}
