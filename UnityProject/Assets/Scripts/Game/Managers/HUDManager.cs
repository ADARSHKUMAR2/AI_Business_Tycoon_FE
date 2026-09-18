using UnityEngine;
using UnityEngine.UI;
using TMPro;
using AIBusinessTycoon.Data;

namespace AIBusinessTycoon.UI
{
    public class HUDManager : MonoBehaviour
    {
        public static HUDManager Instance { get; private set; }
        
        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI levelText;
        
        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI businessCountText;
        [SerializeField] private TextMeshProUGUI landTilesText;
        [SerializeField] private TextMeshProUGUI revenueText;
        
        [Header("Status")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject loadingPanel;
        
        [Header("Settings")]
        [SerializeField] private bool showDebugInfo = true;
        
        private Managers.GameManager gameManager;
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        private void Start()
        {
            gameManager = Managers.GameManager.Instance;
            if (gameManager == null) return;
            
            gameManager.OnPlayerDataLoaded += OnPlayerDataLoaded;
            gameManager.OnPlayerDataUpdated += OnPlayerDataUpdated;
            
            ShowLoading(true);
            UpdateStatus("Initializing game...");
        }
        
        private void OnDestroy()
        {
            if (gameManager != null)
            {
                gameManager.OnPlayerDataLoaded -= OnPlayerDataLoaded;
                gameManager.OnPlayerDataUpdated -= OnPlayerDataUpdated;
            }
        }
        
        private void OnPlayerDataLoaded(PlayerTycoonData player)
        {
            UpdateAllUI(player);
            ShowLoading(false);
            UpdateStatus("Ready to build!");
        }
        
        private void OnPlayerDataUpdated(PlayerTycoonData player)
        {
            UpdateAllUI(player);
        }

        public void ShowHUD(bool show)
        {
            // Toggles the entire HUD visual container on/off
            if (transform.childCount > 0)
            {
                // The first child is usually "HUD_Visuals" created by the Editor script
                transform.GetChild(0).gameObject.SetActive(show);
            }
        }

        
        private void UpdateAllUI(PlayerTycoonData player)
        {
            if (player == null) return;
            
            if (playerNameText != null) playerNameText.text = player.name;
            if (moneyText != null) moneyText.text = $"Rs.{player.money:N0}"; 
            if (levelText != null) levelText.text = $"Level {player.Level}";
            
            if (businessCountText != null) businessCountText.text = $"Businesses: {player.OwnedBusinessCount}";
            if (landTilesText != null) landTilesText.text = $"Land Tiles: {player.stats.land_tiles_owned}";
            if (revenueText != null) revenueText.text = $"Revenue: Rs.{player.stats.total_revenue:N0}"; 
        }
        
        public void UpdateStatus(string status)
        {
            if (statusText != null) statusText.text = status;
        }
        
        public void ShowLoading(bool show)
        {
            if (loadingPanel != null) loadingPanel.SetActive(show);
        }
        
        public void UpdateMoney(float amount)
        {
            if (moneyText != null) moneyText.text = $"Rs.{amount:N0}";
        }

        // --- RESTORED MISSING METHODS ---
        
        public void ShowNotification(string message, float duration = 3f)
        {
            if (statusText != null)
            {
                StartCoroutine(ShowNotificationCoroutine(message, duration));
            }
        }
        
        private System.Collections.IEnumerator ShowNotificationCoroutine(string message, float duration)
        {
            string originalStatus = "Ready to build!";
            statusText.text = message;
            statusText.color = Color.yellow;
            yield return new WaitForSeconds(duration);
            statusText.text = originalStatus;
            statusText.color = Color.white;
        }
        
        public void FlashMoneyChange(float oldAmount, float newAmount)
        {
            if (moneyText == null) return;
            Color originalColor = moneyText.color;
            Color flashColor = newAmount > oldAmount ? Color.green : Color.red;
            StartCoroutine(FlashTextCoroutine(moneyText, originalColor, flashColor));
        }
        
        private System.Collections.IEnumerator FlashTextCoroutine(TextMeshProUGUI text, Color original, Color flash)
        {
            text.color = flash;
            yield return new WaitForSeconds(0.3f);
            text.color = original;
        }

        private void OnGUI()
        {
            if (!showDebugInfo || gameManager == null || gameManager.CurrentPlayer == null) return;
            
            GUIStyle debugStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                normal = { textColor = new Color(1, 1, 1, 0.7f) }
            };
            
            GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 150));
            GUILayout.Label("=== DEBUG INFO ===", new GUIStyle(debugStyle) { fontStyle = FontStyle.Bold });
            GUILayout.Label($"Player ID: {gameManager.CurrentPlayer.player_id}", debugStyle);
            GUILayout.Label($"FPS: {(int)(1f / Time.unscaledDeltaTime)}", debugStyle);
            GUILayout.Label($"Game Ready: {gameManager.IsGameReady}", debugStyle);
            GUILayout.Label($"Loading: {gameManager.IsLoading}", debugStyle);
            GUILayout.EndArea();
        }
    }
}
