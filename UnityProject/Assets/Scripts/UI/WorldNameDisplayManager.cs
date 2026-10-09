using TMPro;
using UnityEngine;
using AIBusinessTycoon.Data;
using AIBusinessTycoon.Managers;

namespace AIBusinessTycoon.UI
{
    /// <summary>
    /// Shows the player name in the macro/bird's-eye view and the active
    /// business name while the player is inside a store.
    /// </summary>
    public class WorldNameDisplayManager : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private GameObject playerNameObject;
        [SerializeField] private GameObject storeNameObject;
        [SerializeField] private TMP_Text playerNameText;
        [SerializeField] private TMP_Text storeNameText;

        private GameManager gameManager;

        private void Start()
        {
            gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogWarning("[WorldNameDisplayManager] GameManager is not available.");
                return;
            }

            gameManager.OnPlayerDataLoaded += OnPlayerDataLoaded;
            gameManager.OnPlayerDataUpdated += OnPlayerDataUpdated;
            gameManager.OnEnteredStore += ShowStoreName;
            gameManager.OnExitedStore += ShowPlayerName;

            OnPlayerDataLoaded(gameManager.CurrentPlayer);
            if (gameManager.IsInStore)
                ShowStoreName();
            else
                ShowPlayerName();
        }

        private void OnDestroy()
        {
            if (gameManager == null) return;

            gameManager.OnPlayerDataLoaded -= OnPlayerDataLoaded;
            gameManager.OnPlayerDataUpdated -= OnPlayerDataUpdated;
            gameManager.OnEnteredStore -= ShowStoreName;
            gameManager.OnExitedStore -= ShowPlayerName;
        }

        private void OnPlayerDataLoaded(PlayerTycoonData player)
        {
            UpdatePlayerName(player);
        }

        private void OnPlayerDataUpdated(PlayerTycoonData player)
        {
            UpdatePlayerName(player);
        }

        private void UpdatePlayerName(PlayerTycoonData player)
        {
            if (playerNameText != null && player != null)
                playerNameText.text = player.name;
        }

        private void ShowPlayerName()
        {
            SetVisible(playerNameObject, true);
            SetVisible(storeNameObject, false);
        }

        private void ShowStoreName()
        {
            BusinessData business = gameManager?.CurrentStore?.BusinessData;
            if (business == null)
            {
                ShowPlayerName();
                return;
            }

            if (storeNameText != null)
                storeNameText.text = business.name;

            SetVisible(playerNameObject, false);
            SetVisible(storeNameObject, true);
        }

        private static void SetVisible(GameObject target, bool visible)
        {
            if (target != null)
                target.SetActive(visible);
        }
    }
}