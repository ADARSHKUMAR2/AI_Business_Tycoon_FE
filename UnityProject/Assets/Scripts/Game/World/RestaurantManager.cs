using System;
using System.Collections.Generic;
using UnityEngine;

public class RestaurantManager : MonoBehaviour
{
    [Header("Plots")]
    [SerializeField] private List<IngredientPlot> ingredientPlots = new List<IngredientPlot>();

    private Dictionary<IngredientType, int> ingredientStock;

    private void Awake()
    {
        ingredientStock = new Dictionary<IngredientType, int>();

        foreach (IngredientType ingredient in Enum.GetValues(typeof(IngredientType)))
        {
            ingredientStock[ingredient] = 0;
        }
    }

    private void Start()
    {
        RegisterExistingPlots();
        StartAllGrowth();
    }

    private void RegisterExistingPlots()
    {
        if (ingredientPlots == null)
            ingredientPlots = new List<IngredientPlot>();

        IngredientPlot[] plotsInChildren = GetComponentsInChildren<IngredientPlot>();
        foreach (var plot in plotsInChildren)
        {
            if (!ingredientPlots.Contains(plot))
                ingredientPlots.Add(plot);
        }
    }

    public void RegisterPlot(IngredientPlot plot)
    {
        if (plot == null)
            return;

        if (!ingredientPlots.Contains(plot))
            ingredientPlots.Add(plot);

        if (!ingredientStock.ContainsKey(plot.IngredientType))
            ingredientStock[plot.IngredientType] = 0;
    }

    public void StartAllGrowth()
    {
        foreach (var plot in ingredientPlots)
        {
            if (plot != null)
                plot.StartGrowing();
        }
    }

    public bool TryHarvest(IngredientPlot plot, out int amount)
    {
        amount = 0;

        if (plot == null)
            return false;

        if (!plot.IsReady)
            return false;

        amount = plot.Harvest();
        ingredientStock[plot.IngredientType] += amount;

        Debug.Log($"[RestaurantManager] Added {amount} {plot.IngredientType}. Stock now: {ingredientStock[plot.IngredientType]}");

        return true;
    }

    public int GetIngredientStock(IngredientType ingredientType)
    {
        if (ingredientStock.ContainsKey(ingredientType))
            return ingredientStock[ingredientType];

        return 0;
    }

    public bool TryConsumeIngredient(IngredientType ingredientType, int amount)
    {
        if (!ingredientStock.ContainsKey(ingredientType))
            return false;

        if (ingredientStock[ingredientType] < amount)
            return false;

        ingredientStock[ingredientType] -= amount;
        return true;
    }
}
