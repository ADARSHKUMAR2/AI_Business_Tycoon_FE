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
    /// Uses UnityWebRequest for async operations with generic request/response handling.
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
        
        #region Generic HTTP Methods
        
        /// <summary>
        /// Generic GET request with JSON deserialization.
        /// </summary>
        private IEnumerator GetRequest<T>(string url, Action<T> onSuccess, Action<string> onError)
        {
            Debug.Log($"[TycoonAPIService] GET request to: {url}");
            
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
                        Debug.Log($"[TycoonAPIService] Response: {jsonResponse}");
                        
                        T data = JsonUtility.FromJson<T>(jsonResponse);
                        
                        if (data != null)
                        {
                            onSuccess?.Invoke(data);
                        }
                        else
                        {
                            string errorMsg = "Failed to parse response data";
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
        /// Generic GET request returning raw string response (for arrays or custom parsing).
        /// </summary>
        private IEnumerator GetRequestRaw(string url, Action<string> onSuccess, Action<string> onError)
        {
            Debug.Log($"[TycoonAPIService] GET request (raw) to: {url}");
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = requestTimeout;
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;
                    Debug.Log($"[TycoonAPIService] Response: {jsonResponse}");
                    onSuccess?.Invoke(jsonResponse);
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
        /// Generic POST request with JSON request body and response deserialization.
        /// </summary>
        private IEnumerator PostRequest<TRequest, TResponse>(
            string url, 
            TRequest requestData, 
            Action<TResponse> onSuccess, 
            Action<string> onError)
        {
            Debug.Log($"[TycoonAPIService] POST request to: {url}");
            
            string jsonData = JsonUtility.ToJson(requestData);
            Debug.Log($"[TycoonAPIService] Request body: {jsonData}");
            
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
                    try
                    {
                        string jsonResponse = request.downloadHandler.text;
                        Debug.Log($"[TycoonAPIService] Response: {jsonResponse}");
                        
                        TResponse data = JsonUtility.FromJson<TResponse>(jsonResponse);
                        onSuccess?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        string errorMsg = $"Failed to parse response: {e.Message}";
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
        /// Generic POST request with JSON request body and boolean success callback.
        /// </summary>
        private IEnumerator PostRequestSimple<TRequest>(
            string url, 
            TRequest requestData, 
            Action<bool> onComplete)
        {
            Debug.Log($"[TycoonAPIService] POST request to: {url}");
            
            string jsonData = JsonUtility.ToJson(requestData);
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
                    Debug.Log($"[TycoonAPIService] Request successful");
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[TycoonAPIService] Request failed: {request.error}");
                    onComplete?.Invoke(false);
                }
            }
        }

        /// <summary>
        /// Generic PUT request that only returns success/fail boolean.
        /// </summary>
        private IEnumerator PutRequestSimple<TRequest>(string url, TRequest payload, Action<bool> onComplete)
        {
            Debug.Log($"[TycoonAPIService] PUT request to: {url}");

            string jsonData = JsonUtility.ToJson(payload);
            
            using (UnityWebRequest request = UnityWebRequest.Put(url, jsonData))
            {
                request.timeout = requestTimeout;
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[TycoonAPIService] PUT request successful");
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[TycoonAPIService] Request failed: {request.error}\nURL: {url}");
                    onComplete?.Invoke(false);
                }
            }
        }

        
        /// <summary>
        /// Generic PUT request with JSON request body and response deserialization.
        /// </summary>
        private IEnumerator PutRequest<TRequest, TResponse>(
            string url, 
            TRequest requestData, 
            Action<TResponse> onSuccess, 
            Action<string> onError)
        {
            Debug.Log($"[TycoonAPIService] PUT request to: {url}");
            
            string jsonData = JsonUtility.ToJson(requestData);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            
            using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = requestTimeout;
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonResponse = request.downloadHandler.text;
                        Debug.Log($"[TycoonAPIService] Response: {jsonResponse}");
                        
                        TResponse data = JsonUtility.FromJson<TResponse>(jsonResponse);
                        onSuccess?.Invoke(data);
                    }
                    catch (Exception e)
                    {
                        string errorMsg = $"Failed to parse response: {e.Message}";
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
        
        #endregion
        
        #region Player APIs
        
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
            
            string url = backendConfig.GetPlayerDataURL(playerId);
            StartCoroutine(GetRequest(url, onSuccess, onError));
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

            string url = backendConfig.GetUpdatePlayerDataURL(data.player_id);
            StartCoroutine(PutRequestSimple(url, data, onComplete));
        }
        
        #endregion
        
        #region Business APIs
        
        /// <summary>
        /// Creates a new business at the specified position.
        /// </summary>
        public void CreateBusiness(BusinessCreateRequest request, Action<BusinessData> onSuccess, Action<string> onError)
        {
            if (backendConfig == null)
            {
                onError?.Invoke("BackendConfig is not assigned!");
                return;
            }
            
            string url = backendConfig.GetActiveURL() + "/api/game/business/create";
            StartCoroutine(PostRequest(url, request, onSuccess, onError));
        }
        
        /// <summary>
        /// Gets business details by ID.
        /// </summary>
        public void GetBusiness(string playerId, string businessId, Action<BusinessData> onSuccess, Action<string> onError)
        {
            if (backendConfig == null)
            {
                onError?.Invoke("BackendConfig is not assigned!");
                return;
            }
            
            string url = $"{backendConfig.GetActiveURL()}/api/game/business/{playerId}/{businessId}";
            StartCoroutine(GetRequest(url, onSuccess, onError));
        }
        
        /// <summary>
        /// Updates inventory for a business.
        /// </summary>
        public void UpdateInventory(string playerId, string businessId, InventoryUpdate update, Action<BusinessData> onSuccess, Action<string> onError)
        {
            if (backendConfig == null)
            {
                onError?.Invoke("BackendConfig is not assigned!");
                return;
            }
            
            string url = $"{backendConfig.GetActiveURL()}/api/game/business/{playerId}/{businessId}/inventory";
            StartCoroutine(PutRequest(url, update, onSuccess, onError));
        }
        
        /// <summary>
        /// Updates price multiplier for a business.
        /// </summary>
        public void UpdatePriceMultiplier(string playerId, string businessId, PriceUpdate update, Action<BusinessData> onSuccess, Action<string> onError)
        {
            if (backendConfig == null)
            {
                onError?.Invoke("BackendConfig is not assigned!");
                return;
            }
            
            string url = $"{backendConfig.GetActiveURL()}/api/game/business/{playerId}/{businessId}/price";
            StartCoroutine(PutRequest(url, update, onSuccess, onError));
        }
        
        #endregion
        
        #region Land APIs
        
        /// <summary>
        /// Gets available adjacent land tiles for purchase.
        /// Returns raw JSON string (array of available tiles).
        /// </summary>
        public void GetAvailableLand(string playerId, Action<string> onSuccess, Action<string> onError)
        {
            if (backendConfig == null)
            {
                onError?.Invoke("BackendConfig is not assigned!");
                return;
            }
            
            string url = $"{backendConfig.GetActiveURL()}/api/game/land/{playerId}/available";
            StartCoroutine(GetRequestRaw(url, onSuccess, onError));
        }
        
        /// <summary>
        /// Purchases a land tile at the specified position.
        /// </summary>
        public void PurchaseLand(LandPurchaseRequest request, Action<LandTile> onSuccess, Action<string> onError)
        {
            if (backendConfig == null)
            {
                onError?.Invoke("BackendConfig is not assigned!");
                return;
            }
            
            string url = backendConfig.GetActiveURL() + "/api/game/land/purchase";
            StartCoroutine(PostRequest(url, request, onSuccess, onError));
        }
        
        #endregion
        
        #region Employee APIs
        
        /// <summary>
        /// Hire a new employee for a business.
        /// </summary>
        public void HireEmployee(string playerId, string businessId, EmployeeHireRequest request, Action<Employee> onSuccess, Action<string> onError)
        {
            if (backendConfig == null)
            {
                onError?.Invoke("BackendConfig is not assigned!");
                return;
            }
            
            string url = $"{backendConfig.GetActiveURL()}/api/game/employee/{playerId}/{businessId}/hire";
            StartCoroutine(PostRequest(url, request, onSuccess, onError));
        }
        
        /// <summary>
        /// Fire an employee from a business.
        /// </summary>
        public void FireEmployee(string playerId, string businessId, string employeeId, Action<bool> onComplete)
        {
            if (backendConfig == null)
            {
                Debug.LogError("[TycoonAPIService] BackendConfig is not assigned!");
                onComplete?.Invoke(false);
                return;
            }
            
            // For DELETE requests, we can use a simple structure
            string url = $"{backendConfig.GetActiveURL()}/api/game/employee/{playerId}/{businessId}/{employeeId}";
            StartCoroutine(DeleteRequest(url, onComplete));
        }
        
        /// <summary>
        /// Generic DELETE request.
        /// </summary>
        private IEnumerator DeleteRequest(string url, Action<bool> onComplete)
        {
            Debug.Log($"[TycoonAPIService] DELETE request to: {url}");
            
            using (UnityWebRequest request = UnityWebRequest.Delete(url))
            {
                request.timeout = requestTimeout;
                request.SetRequestHeader("Content-Type", "application/json");
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[TycoonAPIService] Delete successful");
                    onComplete?.Invoke(true);
                }
                else
                {
                    Debug.LogError($"[TycoonAPIService] Delete failed: {request.error}");
                    onComplete?.Invoke(false);
                }
            }
        }
        
        #endregion
    }
}
