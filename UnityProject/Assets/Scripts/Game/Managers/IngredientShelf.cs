using System.Collections;
using UnityEngine;

namespace AIBusinessTycoon.Managers
{
    public class IngredientShelf : MonoBehaviour
    {
        [SerializeField] private string ingredientId;
        
        [Header("Stock Settings")]
        [SerializeField] private int maxStock = 3;
        [SerializeField] private float respawnTime = 30f;
        
        private int currentStock;
        private bool isRespawning = false;

        public string IngredientId => ingredientId;
        
        // Tells the player if there are items left to take
        public bool HasStock => currentStock > 0;

        private void Start()
        {
            // Start the game with full stock
            currentStock = maxStock;
            UpdateVisuals();
        }

        // Called by the Player when they press 'E'
        public bool TryTakeItem()
        {
            if (currentStock <= 0) 
            {
                Debug.Log($"{ingredientId} is out of stock! Waiting for respawn.");
                return false;
            }
            
            // Decrease the stock by 1
            currentStock--;
            UpdateVisuals();

            // If we just hit 0, start the 30 second timer
            if (currentStock == 0 && !isRespawning)
            {
                StartCoroutine(RespawnRoutine());
            }
            
            return true; // Successfully took an item
        }

        private IEnumerator RespawnRoutine()
        {
            isRespawning = true;
            Debug.Log($"{ingredientId} is empty. Respawning in {respawnTime} seconds...");
            
            // Wait for 30 seconds
            yield return new WaitForSeconds(respawnTime);
            
            // Refill the stock!
            currentStock = maxStock;
            isRespawning = false;
            UpdateVisuals();
            
            Debug.Log($"{ingredientId} has respawned!");
        }

        private void UpdateVisuals()
        {
            // Optional: If you want to update 3D text floating above the pad!
            // You can find the TextMesh in the children and update it to show the count.
            TextMesh textMesh = GetComponentInChildren<TextMesh>();
            if (textMesh != null)
            {
                if (isRespawning)
                    textMesh.text = "0"; // or "Wait..."
                else
                    textMesh.text = currentStock.ToString();
            }
        }
    }
}
