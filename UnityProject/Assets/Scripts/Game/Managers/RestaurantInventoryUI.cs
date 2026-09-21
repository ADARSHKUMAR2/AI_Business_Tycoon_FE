using TMPro;
using UnityEngine;
using AIBusinessTycoon.Managers; 

public class RestaurantInventoryUI : MonoBehaviour
{
    [SerializeField] private RestaurantManager restaurantManager;
    [SerializeField] private TMP_Text tomatoText;
    [SerializeField] private TMP_Text onionText;
    [SerializeField] private TMP_Text lettuceText;
    [SerializeField] private TMP_Text herbText;
    [SerializeField] private TMP_Text flourText;
    [SerializeField] private TMP_Text riceText;

    private void Start()
    {
        if (restaurantManager == null)
            restaurantManager = FindObjectOfType<RestaurantManager>();
    }

    private void Update()
    {
        if (restaurantManager == null)
            return;

        UpdateText(tomatoText, IngredientType.Tomato);
        UpdateText(onionText, IngredientType.Onion);
        UpdateText(lettuceText, IngredientType.Lettuce);
        UpdateText(herbText, IngredientType.Herb);
        UpdateText(flourText, IngredientType.Flour);
        UpdateText(riceText, IngredientType.Rice);
    }

    private void UpdateText(TMP_Text text, IngredientType ingredientType)
    {
        if (text == null)
            return;

        int count = restaurantManager.GetIngredientStock(ingredientType);
        text.text = $"{ingredientType}: {count}";
    }
}
