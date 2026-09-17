using UnityEngine;

namespace AIBusinessTycoon.Config
{
    /// <summary>
    /// ScriptableObject to manage backend API endpoints.
    /// Create via: Assets > Create > AI Business Tycoon > Backend Config
    /// </summary>
    [CreateAssetMenu(fileName = "BackendConfig", menuName = "AI Business Tycoon/Backend Config", order = 1)]
    public class BackendConfig : ScriptableObject
    {
        [Header("Backend URLs")]
        [Tooltip("Local development backend URL")]
        public string localBackendURL = "http://localhost:8000";
        
        [Tooltip("Production backend URL")]
        public string productionBackendURL = "https://api.yourgame.com";
        
        [Header("Environment Settings")]
        [Tooltip("Toggle to use local backend instead of production")]
        public bool useLocalBackend = true;
        
        [Header("API Endpoints")]
        public string getPlayerDataEndpoint = "/api/game/player/{0}";
        public string updatePlayerDataEndpoint = "/api/game/player/{0}";
        
        /// <summary>
        /// Returns the active backend URL based on environment setting.
        /// </summary>
        public string GetActiveURL()
        {
            return useLocalBackend ? localBackendURL : productionBackendURL;
        }
        
        /// <summary>
        /// Builds the full URL for getting player data.
        /// </summary>
        public string GetPlayerDataURL(string playerId)
        {
            return GetActiveURL() + string.Format(getPlayerDataEndpoint, playerId);
        }
        
        /// <summary>
        /// Builds the full URL for updating player data.
        /// </summary>
        public string GetUpdatePlayerDataURL()
        {
            return GetActiveURL() + updatePlayerDataEndpoint;
        }
    }
}
