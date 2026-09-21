using System;
using System.Collections.Generic;
using UnityEngine;
using AIBusinessTycoon.Managers; 

public class RestaurantManager : MonoBehaviour
{
    [Header("Plots")]
    [SerializeField] private List<IngredientPlot> ingredientPlots = new List<IngredientPlot>();

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
        if (plot == null) return;

        if (!ingredientPlots.Contains(plot))
            ingredientPlots.Add(plot);
    }

    public void StartAllGrowth()
    {
        foreach (var plot in ingredientPlots)
        {
            if (plot != null)
                plot.StartGrowing();
        }
    }

    // Tells AI Customers exactly how much physical stock is sitting on the plots
    public int GetIngredientStock(IngredientType ingredientType)
    {
        int totalStock = 0;
        foreach (var plot in ingredientPlots)
        {
            if (plot != null && plot.IngredientType == ingredientType)
            {
                totalStock += plot.CurrentStock;
            }
        }
        return totalStock;
    }

    // Called by Customers! Now it actually takes the item off the physical plot.
    public bool TryConsumeIngredient(IngredientType ingredientType, int amount)
    {
        // First check if we have enough total stock across all plots
        if (GetIngredientStock(ingredientType) < amount)
        {
            return false;
        }

        int amountTaken = 0;

        // Loop through all plots and take the items one by one
        foreach (var plot in ingredientPlots)
        {
            if (plot != null && plot.IngredientType == ingredientType)
            {
                while (plot.CurrentStock > 0 && amountTaken < amount)
                {
                    // This calls the method on the plot, triggering your Debug.Log!
                    if (plot.TryTakeOne())
                    {
                        amountTaken++;
                    }
                }
            }

            if (amountTaken >= amount)
            {
                return true; // The customer got all their items!
            }
        }

        return false;
    }

    // Kept just in case the player can still manually harvest things
    public bool TryHarvest(IngredientPlot plot, out int amount)
    {
        amount = 0;
        if (plot == null || !plot.IsReady) return false;

        if (plot.TryTakeOne())
        {
            amount = 1;
            return true;
        }
        return false;
    }
}
