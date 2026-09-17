using System;
using System.Collections.Generic;

namespace AIBusinessTycoon.Data
{
    /// <summary>
    /// Represents a single inventory item in a business.
    /// Mirrors backend InventoryItem model.
    /// </summary>
    [Serializable]
    public class InventoryItem
    {
        public string name;
        public float cost;
        public float price;
        public int stock;
        public int total_sold;

        public float CalculateProfit()
        {
            return price - cost;
        }

        public float CalculateProfitMargin()
        {
            if (cost == 0) return 0f;
            return ((price - cost) / cost) * 100f;
        }

        public float CalculateTotalValue()
        {
            return stock * cost;
        }

        public override string ToString()
        {
            return $"{name} | Stock: {stock} | Price: ₹{price} | Sold: {total_sold}";
        }
    }

    /// <summary>
    /// Request to update inventory (restock or change price).
    /// Mirrors backend InventoryUpdate schema.
    /// </summary>
    [Serializable]
    public class InventoryUpdate
    {
        public string item_name;
        public int restock_quantity;
        public float new_price;

        public InventoryUpdate(string itemName, int restockQty, float newPrice)
        {
            item_name = itemName;
            restock_quantity = restockQty;
            new_price = newPrice;
        }
    }

    /// <summary>
    /// Request to update business price multiplier.
    /// </summary>
    [Serializable]
    public class PriceUpdate
    {
        public float multiplier;

        public PriceUpdate(float priceMultiplier)
        {
            multiplier = priceMultiplier;
        }
    }
}
