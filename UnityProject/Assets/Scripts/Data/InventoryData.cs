using System;

namespace AIBusinessTycoon.Data
{
    /// <summary>
    /// Represents a single inventory item in a business.
    /// Mirrors backend InventoryItem model.
    /// Phase 3: Added max_stock field.
    /// </summary>
    [Serializable]
    public class InventoryItem
    {
        public string name;
        public float  cost;
        public float  price;
        public int    stock;
        public int    max_stock  = 10; // Phase 3: NEW — shelf capacity (10, 20, or 30)
        public int    total_sold;

        public float CalculateProfit()        => price - cost;
        public float CalculateProfitMargin()  => cost == 0 ? 0f : ((price - cost) / cost) * 100f;
        public float CalculateTotalValue()    => stock * cost;

        // Phase 3: What percentage of the shelf is filled?
        public float StockPercent => max_stock > 0 ? (float)stock / max_stock : 0f;
        public bool  IsEmpty      => stock <= 0;
        public bool  IsFull       => stock >= max_stock;

        public override string ToString()
        {
            return $"{name} | Stock: {stock}/{max_stock} | Price: ₹{price} | Sold: {total_sold}";
        }
    }

    /// <summary>
    /// Request to update inventory stock or price.
    /// Mirrors backend InventoryUpdate schema EXACTLY.
    /// FIXED: was item_name/restock_quantity — now item_key/stock/price.
    /// </summary>
    [Serializable]
    public class InventoryUpdate
    {
        public string item_key;  // e.g., "rice"
        public int    stock;     // New total stock quantity
        public float  price;     // New selling price

        public InventoryUpdate(string itemKey, int newStock, float newPrice)
        {
            item_key = itemKey;
            stock    = newStock;
            price    = newPrice;
        }
    }

    /// <summary>
    /// Request to update the global price multiplier for a business.
    /// </summary>
    [Serializable]
    public class PriceUpdate
    {
        public float price_multiplier;

        public PriceUpdate(float multiplier)
        {
            price_multiplier = multiplier;
        }
    }

    /// <summary>
    /// Phase 3: Request to upgrade a shelf's maximum capacity.
    /// Mirrors backend ShelfUpgradeRequest.
    /// target_capacity must be 20 or 30.
    /// </summary>
    [Serializable]
    public class ShelfUpgradeRequest
    {
        public string item_key;
        public int    target_capacity; // Must be 20 or 30

        public ShelfUpgradeRequest(string itemKey, int targetCap)
        {
            item_key         = itemKey;
            target_capacity  = targetCap;
        }
    }
}
