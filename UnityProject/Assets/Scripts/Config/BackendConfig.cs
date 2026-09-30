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
        
        [Header("Event Endpoints")]
        public string getActiveEventEndpoint = "/api/game/events/active?player_id={0}";
        public string registerEventEndpoint = "/api/game/events/{0}/register/{1}";
        
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
        public string GetUpdatePlayerDataURL(string playerId)
        {
            return string.Format(GetActiveURL() + updatePlayerDataEndpoint, playerId);
        }
        
        /// <summary>
        /// Builds the full URL for getting active event.
        /// </summary>
        public string GetActiveEventURL(string playerId)
        {
            return GetActiveURL() + string.Format(getActiveEventEndpoint, playerId);
        }
        
        /// <summary>
        /// Builds the full URL for registering for an event.
        /// </summary>
        public string GetRegisterEventURL(string eventId, string playerId)
        {
            return GetActiveURL() + string.Format(registerEventEndpoint, eventId, playerId);
        }
    }
}
