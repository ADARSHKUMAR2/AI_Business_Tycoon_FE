using UnityEngine;

[RequireComponent(typeof(IngredientPlot))]
public class IngredientPlotInteractable : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private RestaurantManager restaurantManager;
    [SerializeField] private string interactionPrompt = "Harvest";

    private IngredientPlot plot;

    public string InteractionPrompt => interactionPrompt;
    public bool CanInteract => plot != null && plot.IsReady;

    private void Awake()
    {
        plot = GetComponent<IngredientPlot>();

        if (restaurantManager == null)
        {
            restaurantManager = FindObjectOfType<RestaurantManager>();
        }
    }

    public void Interact()
    {
        if (plot == null)
            return;

        if (restaurantManager == null)
        {
            Debug.LogWarning($"[IngredientPlotInteractable] No RestaurantManager found on {name}");
            return;
        }

        if (!plot.IsReady)
        {
            Debug.Log($"[IngredientPlotInteractable] {plot.IngredientType} not ready yet.");
            return;
        }

        bool harvested = restaurantManager.TryHarvest(plot, out int amount);

        if (!harvested)
        {
            Debug.Log($"[IngredientPlotInteractable] Failed to harvest {plot.IngredientType}.");
            return;
        }

        Debug.Log($"[IngredientPlotInteractable] Harvested {amount} {plot.IngredientType}.");
    }
}
