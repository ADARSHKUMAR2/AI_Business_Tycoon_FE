using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIBusinessTycoon.Managers
{
    public class CraftingTable : MonoBehaviour
    {
        [SerializeField] private RecipeType recipeType;
        
        private readonly List<string> placedIngredients = new List<string>();
        private readonly List<GameObject> visualItems = new List<GameObject>();
        
        // NEW: Prevents placing new ingredients while the finished food is on display
        private bool isDisplayingFinishedItem = false;

        public bool TryPlaceHeldItem(PlayerInventory inventory)
        {
            // Block placement if the table is currently showing the finished burger/pizza
            if (isDisplayingFinishedItem)
            {
                Debug.Log("Table is busy serving the finished item!");
                return false;
            }

            if (inventory == null || !inventory.HasItem())
            {
                Debug.Log("No item in hand.");
                return false;
            }

            string itemId = inventory.HeldItemId;
            RecipeDefinition recipe = RecipeDatabase.Recipes[recipeType];

            if (!recipe.Ingredients.Contains(itemId))
            {
                Debug.Log("This ingredient does not belong on this table.");
                return false;
            }

            if (placedIngredients.Contains(itemId))
            {
                Debug.Log("This ingredient is already on the table.");
                return false;
            }

            placedIngredients.Add(itemId);
            inventory.DropHeldItem();

            Debug.Log("Placed " + itemId + " on " + recipeType + " table.");

            SpawnVisualItem(itemId, placedIngredients.Count - 1);

            if (IsRecipeComplete())
            {
                Craft();
            }

            return true;
        }

        private void SpawnVisualItem(string itemId, int index)
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(transform);
            
            visual.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            
            float xPos = (index * 0.4f) - 0.6f; 
            visual.transform.localPosition = new Vector3(xPos, 0.8f, 0f); 

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(visual.transform);
            textObj.transform.localPosition = new Vector3(0f, 1.5f, 0f); 
            
            TextMesh textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = itemId;
            textMesh.characterSize = 0.1f;
            textMesh.fontSize = 50;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.black; 

            visualItems.Add(visual);
        }

        private bool IsRecipeComplete()
        {
            RecipeDefinition recipe = RecipeDatabase.Recipes[recipeType];
            return recipe.Ingredients.All(placedIngredients.Contains);
        }

        private void Craft()
        {
            RecipeDefinition recipe = RecipeDatabase.Recipes[recipeType];
            Debug.Log("Crafted: " + recipe.OutputItemName);
            
            placedIngredients.Clear();

            foreach (var obj in visualItems)
            {
                Destroy(obj);
            }
            visualItems.Clear();

            // NEW: Start the routine to show the final product
            StartCoroutine(ShowFinishedItemRoutine(recipe.OutputItemName));
        }

        // NEW: Coroutine to show the finished item and clean it up after a delay
        private IEnumerator ShowFinishedItemRoutine(string itemName)
        {
            isDisplayingFinishedItem = true;

            // Create a larger sphere to represent the finished meal
            GameObject finishedItem = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(finishedItem.GetComponent<Collider>());
            finishedItem.transform.SetParent(transform);
            
            // Make it bigger and center it
            finishedItem.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            finishedItem.transform.localPosition = new Vector3(0f, 0.8f, 0f);

            // Make it yellow/gold so it stands out
            Renderer rend = finishedItem.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.yellow;
            }

            // Add the text label
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(finishedItem.transform);
            textObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            
            TextMesh textMesh = textObj.AddComponent<TextMesh>();
            textMesh.text = itemName.ToUpper() + "!"; // Prints "BURGER!" or "PIZZA!"
            textMesh.characterSize = 0.1f;
            textMesh.fontSize = 65;
            textMesh.fontStyle = FontStyle.Bold;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.blue;

            // Leave it on the table for 3 seconds
            yield return new WaitForSeconds(3f);

            // Clean it up so the table can take the next order
            Destroy(finishedItem);
            isDisplayingFinishedItem = false;
        }
    }
}
