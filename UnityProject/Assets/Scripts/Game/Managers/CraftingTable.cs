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
        
        // Tracks if the food is ready to be picked up by the player
        private bool isFoodReady = false;
        private GameObject finishedFoodVisual;

        // THIS IS THE METHOD PlayerController IS LOOKING FOR!
        public bool TryInteract(PlayerInventory inventory)
        {
            // 1. IF FOOD IS READY: Give it to the player
            if (isFoodReady)
            {
                if (inventory.HasItem())
                {
                    Debug.Log("Your hands are full! Drop your item first.");
                    return false;
                }

                // Give the finished item to the player
                RecipeDefinition recipe = RecipeDatabase.Recipes[recipeType];
                inventory.PickUpItem(recipe.OutputItemName); // e.g. "burger"
                Debug.Log($"Picked up {recipe.OutputItemName}!");

                // Clean up the table
                isFoodReady = false;
                Destroy(finishedFoodVisual);
                return true;
            }

            // 2. IF FOOD IS NOT READY: Try to place an ingredient
            if (inventory == null || !inventory.HasItem())
            {
                Debug.Log("No item in hand.");
                return false;
            }

            string itemId = inventory.HeldItemId;
            RecipeDefinition currentRecipe = RecipeDatabase.Recipes[recipeType];

            if (!currentRecipe.Ingredients.Contains(itemId))
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
            placedIngredients.Clear();

            foreach (var obj in visualItems) Destroy(obj);
            visualItems.Clear();

            // Display the finished food permanently until the player takes it
            isFoodReady = true;
            finishedFoodVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(finishedFoodVisual.GetComponent<Collider>());
            finishedFoodVisual.transform.SetParent(transform);
            finishedFoodVisual.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            finishedFoodVisual.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            finishedFoodVisual.GetComponent<Renderer>().material.color = Color.yellow;
            
            Debug.Log($"{recipe.OutputItemName} is ready for pickup!");
        }
    }
}
