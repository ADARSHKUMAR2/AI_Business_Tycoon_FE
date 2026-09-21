using UnityEngine;

namespace AIBusinessTycoon.Managers
{
    public class IngredientShelf : MonoBehaviour
    {
        [SerializeField] private string ingredientId;

        public string IngredientId => ingredientId;

        public void SetIngredientId(string value)
        {
            ingredientId = value;
        }
    }
}
