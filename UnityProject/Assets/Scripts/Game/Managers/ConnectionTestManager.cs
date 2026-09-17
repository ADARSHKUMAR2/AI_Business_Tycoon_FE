using UnityEngine;
using AIBusinessTycoon.Services;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Config;

namespace AIBusinessTycoon.Managers
{
    public class ConnectionTestManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BackendConfig backendConfig;
        
        [Header("Test Settings")]
        [SerializeField] private string testPlayerId = "test123";
        [SerializeField] private bool autoTestOnStart = true;
        
        private TycoonAPIService apiService;
        
        private void Start()
        {
            Debug.Log("=== Connection Test Manager Started ===");
            
            // Get or create API service
            apiService = TycoonAPIService.Instance;
            
            // Assign backend config to the service via reflection
            if (backendConfig != null)
            {
                var field = apiService.GetType().GetField("backendConfig", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    field.SetValue(apiService, backendConfig);
                    Debug.Log($"BackendConfig assigned: {backendConfig.GetActiveURL()}");
                }
            }
            else
            {
                Debug.LogError("BackendConfig is not assigned in ConnectionTestManager!");
                return;
            }
            
            // Auto-test on start if enabled
            if (autoTestOnStart)
            {
                Invoke(nameof(TestGetPlayerData), 0.5f);
            }
        }
        
        public void TestGetPlayerData()
        {
            Debug.Log($"[Test] Fetching player data for ID: {testPlayerId}");
            
            apiService.GetPlayerData(
                testPlayerId,
                OnPlayerDataReceived,
                OnPlayerDataError
            );
        }
        
        private void OnPlayerDataReceived(PlayerTycoonData data)
        {
            Debug.Log($"[Test] ✅ SUCCESS! Received: {data}");
            Debug.Log($"[Test] Player Name: {data.name}");
            Debug.Log($"[Test] Money: ₹{data.money}");
            Debug.Log($"[Test] Level: {data.Level}");
            Debug.Log($"[Test] Businesses: {data.OwnedBusinessCount}");
        }
        
        private void OnPlayerDataError(string error)
        {
            Debug.LogError($"[Test] ❌ FAILED! Error: {error}");
            Debug.LogError("[Test] Make sure the FastAPI backend is running at http://localhost:8000");
        }
        
        public void TestUpdatePlayerData()
        {
            PlayerTycoonData testData = new PlayerTycoonData
            {
                player_id = testPlayerId,
                name = "UnityTestPlayer",
                money = 99999f,
                stats = new PlayerStats
                {
                    level = 10,
                    businesses_owned = 5
                }
            };
            
            Debug.Log($"[Test] Updating player data: {testData}");
            
            apiService.UpdatePlayerData(testData, (success) =>
            {
                if (success)
                {
                    Debug.Log("[Test] ✅ Player data updated successfully!");
                }
                else
                {
                    Debug.LogError("[Test] ❌ Failed to update player data!");
                }
            });
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 400, 250));
            
            GUILayout.Label("=== AI Business Tycoon - Connection Test ===");
            GUILayout.Space(10);
            
            if (GUILayout.Button("Test GET Player Data", GUILayout.Height(40)))
            {
                TestGetPlayerData();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Test UPDATE Player Data", GUILayout.Height(40)))
            {
                TestUpdatePlayerData();
            }
            
            GUILayout.Space(10);
            GUILayout.Label($"Backend URL: {backendConfig?.GetActiveURL() ?? "Not configured"}");
            GUILayout.Label($"Test Player ID: {testPlayerId}");
            GUILayout.Label($"Check Console for detailed logs");
            
            GUILayout.EndArea();
        }
    }
}
