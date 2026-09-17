using UnityEngine;
using AIBusinessTycoon.Services;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Config;

namespace AIBusinessTycoon.Managers
{
    /// <summary>
    /// Comprehensive test manager for all API endpoints.
    /// Tests Player, Business, and Land APIs with visual UI.
    /// </summary>
    public class APITestManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BackendConfig backendConfig;

        [Header("Test Player ID")]
        [SerializeField] private string testPlayerId = "player_0eb8eddc";

        [Header("Test Controls")]
        [SerializeField] private bool runTestsOnStart = false;

        private TycoonAPIService apiService;
        private PlayerTycoonData currentPlayer;

        private void Start()
        {
            Debug.Log("=== API Test Manager Started ===");

            // Get or create API service
            apiService = TycoonAPIService.Instance;

            // Assign backend config via reflection
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
                Debug.LogError("BackendConfig is not assigned!");
                return;
            }

            if (runTestsOnStart)
            {
                Invoke(nameof(RunAllTests), 0.5f);
            }
        }

        private void Update()
        {
            // Keyboard shortcuts for manual testing
            if (Input.GetKeyDown(KeyCode.Alpha1)) TestGetPlayer();
            if (Input.GetKeyDown(KeyCode.Alpha2)) TestCreateBusiness();
            if (Input.GetKeyDown(KeyCode.Alpha3)) TestGetAvailableLand();
            if (Input.GetKeyDown(KeyCode.Alpha4)) TestPurchaseLand();
            if (Input.GetKeyDown(KeyCode.Alpha5)) RunAllTests();
        }

        #region Test Suite

        public void RunAllTests()
        {
            Debug.Log("\n===========================================");
            Debug.Log("Running Complete API Test Suite");
            Debug.Log("===========================================");
            StartCoroutine(RunTestSequence());
        }

        private System.Collections.IEnumerator RunTestSequence()
        {
            // Test 1: Get Player Data
            Debug.Log("\n--- Test 1: Get Player Data ---");
            bool test1Complete = false;
            TestGetPlayer(() => test1Complete = true);
            yield return new WaitUntil(() => test1Complete);
            yield return new WaitForSeconds(1f);

            // Test 2: Get Available Land
            Debug.Log("\n--- Test 2: Get Available Land ---");
            bool test2Complete = false;
            TestGetAvailableLand(() => test2Complete = true);
            yield return new WaitUntil(() => test2Complete);
            yield return new WaitForSeconds(1f);

            // Test 3: Create Business
            Debug.Log("\n--- Test 3: Create Business ---");
            bool test3Complete = false;
            TestCreateBusiness(() => test3Complete = true);
            yield return new WaitUntil(() => test3Complete);
            yield return new WaitForSeconds(1f);

            Debug.Log("\n===========================================");
            Debug.Log("All Tests Completed!");
            Debug.Log("===========================================");
        }

        #endregion

        #region Individual Tests

        public void TestGetPlayer(System.Action onComplete = null)
        {
            Debug.Log($"[Test] Fetching player data for: {testPlayerId}");

            apiService.GetPlayerData(
                testPlayerId,
                (playerData) => OnPlayerDataReceived(playerData, onComplete),
                OnPlayerDataError
            );
        }

        private void OnPlayerDataReceived(PlayerTycoonData data, System.Action onComplete = null)
        {
            currentPlayer = data;

            Debug.Log($"[Test] SUCCESS! Player Data Received:");
            Debug.Log($"  Player ID: {data.player_id}");
            Debug.Log($"  Name: {data.name}");
            Debug.Log($"  Money: Rs.{data.money:N0}");
            Debug.Log($"  Level: {data.Level}");
            Debug.Log($"  Businesses: {data.OwnedBusinessCount}");
            Debug.Log($"  Land Tiles: {data.stats.land_tiles_owned}");
            Debug.Log($"  Created: {data.created_at}");

            onComplete?.Invoke();
        }

        private void OnPlayerDataError(string error)
        {
            Debug.LogError($"[Test] FAILED! Error: {error}");
            Debug.LogError("[Test] Make sure:");
            Debug.LogError("  1. Backend is running at http://localhost:8000");
            Debug.LogError($"  2. Player '{testPlayerId}' exists in database");
        }

        public void TestCreateBusiness(System.Action onComplete = null)
        {
            if (currentPlayer == null)
            {
                Debug.LogError("[Test] Must fetch player data first! Press '1' first.");
                return;
            }

            Debug.Log($"[Test] Creating Kirana Store at position (0, 0)");

            BusinessCreateRequest request = new BusinessCreateRequest(
                currentPlayer.player_id,
                "kirana",
                "Test Kirana Store",
                0,
                0
            );

            apiService.CreateBusiness(
                request,
                (business) => OnBusinessCreated(business, onComplete),
                OnBusinessCreationError
            );
        }

        private void OnBusinessCreated(BusinessData business, System.Action onComplete = null)
        {
            Debug.Log($"[Test] SUCCESS! Business Created:");
            Debug.Log($"  Business ID: {business.business_id}");
            Debug.Log($"  Name: {business.name}");
            Debug.Log($"  Type: {business.business_type}");
            Debug.Log($"  Position: ({business.position_x}, {business.position_y})");
            Debug.Log($"  Is Open: {business.is_open}");
            Debug.Log($"  Created: {business.created_at}");

            onComplete?.Invoke();
        }

        private void OnBusinessCreationError(string error)
        {
            Debug.LogError($"[Test] FAILED! Error: {error}");
            Debug.LogError("[Test] Possible reasons:");
            Debug.LogError("  1. Tile already has a business");
            Debug.LogError("  2. Player doesn't own the tile");
            Debug.LogError("  3. Insufficient funds");
        }

        public void TestGetAvailableLand(System.Action onComplete = null)
        {
            if (currentPlayer == null)
            {
                Debug.LogError("[Test] Must fetch player data first! Press '1' first.");
                return;
            }

            Debug.Log($"[Test] Fetching available land for player: {currentPlayer.player_id}");

            apiService.GetAvailableLand(
                currentPlayer.player_id,
                (jsonResponse) => OnAvailableLandReceived(jsonResponse, onComplete),
                OnAvailableLandError
            );
        }

        private void OnAvailableLandReceived(string jsonResponse, System.Action onComplete = null)
        {
            Debug.Log($"[Test] SUCCESS! Available Land:");
            Debug.Log($"  Raw JSON: {jsonResponse}");
            Debug.Log($"[Test] Available tiles are adjacent to owned tiles");

            onComplete?.Invoke();
        }

        private void OnAvailableLandError(string error)
        {
            Debug.LogError($"[Test] FAILED! Error: {error}");
        }

        public void TestPurchaseLand(System.Action onComplete = null)
        {
            if (currentPlayer == null)
            {
                Debug.LogError("[Test] Must fetch player data first! Press '1' first.");
                return;
            }

            Debug.Log($"[Test] Purchasing land at position (1, 0)");

            LandPurchaseRequest request = new LandPurchaseRequest(
                currentPlayer.player_id,
                new Position { x = 1, y = 0 }
            );

            apiService.PurchaseLand(
                request,
                (tile) => OnLandPurchased(tile, onComplete),
                OnLandPurchaseError
            );
        }

        private void OnLandPurchased(LandTile tile, System.Action onComplete = null)
        {
            Debug.Log($"[Test] SUCCESS! Land Purchased:");
            Debug.Log($"  Position: ({tile.position.x}, {tile.position.y})");
            Debug.Log($"  Type: {tile.tile_type}");
            Debug.Log($"  Cost: Rs.{tile.purchase_cost:N0}");
            Debug.Log($"  Purchased: {tile.purchased_at}");

            onComplete?.Invoke();
        }

        private void OnLandPurchaseError(string error)
        {
            Debug.LogError($"[Test] FAILED! Error: {error}");
            Debug.LogError("[Test] Possible reasons:");
            Debug.LogError("  1. Tile not adjacent to owned land");
            Debug.LogError("  2. Insufficient funds");
            Debug.LogError("  3. Tile already owned");
        }

        #endregion

        #region UI Display

        private void OnGUI()
        {
            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.cyan }
            };

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                padding = new RectOffset(10, 10, 10, 10)
            };

            GUIStyle infoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            GUILayout.BeginArea(new Rect(10, 10, 450, 600));

            GUILayout.Label("API TEST MANAGER", titleStyle);
            GUILayout.Space(10);

            GUILayout.Label($"Backend: {backendConfig?.GetActiveURL() ?? "Not configured"}", infoStyle);
            GUILayout.Label($"Test Player: {testPlayerId}", infoStyle);
            GUILayout.Space(10);

            if (currentPlayer != null)
            {
                GUILayout.Label($"Money: Rs.{currentPlayer.money:N0}", new GUIStyle(infoStyle) { fontStyle = FontStyle.Bold });
                GUILayout.Label($"Businesses: {currentPlayer.OwnedBusinessCount}", infoStyle);
                GUILayout.Label($"Land Tiles: {currentPlayer.stats.land_tiles_owned}", infoStyle);
            }
            else
            {
                GUILayout.Label("No player data loaded. Press [1] to fetch.", new GUIStyle(infoStyle) { normal = { textColor = Color.yellow } });
            }

            GUILayout.Space(20);

            if (GUILayout.Button("[1] Get Player Data", buttonStyle, GUILayout.Height(45)))
            {
                TestGetPlayer();
            }

            if (GUILayout.Button("[2] Create Business (at 0,0)", buttonStyle, GUILayout.Height(45)))
            {
                TestCreateBusiness();
            }

            if (GUILayout.Button("[3] Get Available Land", buttonStyle, GUILayout.Height(45)))
            {
                TestGetAvailableLand();
            }

            if (GUILayout.Button("[4] Purchase Land (at 1,0)", buttonStyle, GUILayout.Height(45)))
            {
                TestPurchaseLand();
            }

            GUILayout.Space(10);

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("[5] RUN ALL TESTS", buttonStyle, GUILayout.Height(60)))
            {
                RunAllTests();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.Space(20);
            GUILayout.Label("Keyboard shortcuts: 1-5 keys", new GUIStyle(infoStyle) { fontSize = 10, normal = { textColor = Color.gray } });

            GUILayout.EndArea();
        }

        #endregion
    }
}
