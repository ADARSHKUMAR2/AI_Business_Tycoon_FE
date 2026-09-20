using System;
using System.Collections.Generic;

namespace AIBusinessTycoon.Data
{
    [Serializable]
    public class InventoryItem
    {
        public string name;
        public float  cost;
        public float  price;
        public int    stock;
        public int    max_stock  = 10; 
        public int    total_sold;

        public float CalculateProfit()        => price - cost;
        public float CalculateProfitMargin()  => cost == 0 ? 0f : ((price - cost) / cost) * 100f;
        public float CalculateTotalValue()    => stock * cost;

        public float StockPercent => max_stock > 0 ? (float)stock / max_stock : 0f;
        public bool  IsEmpty      => stock <= 0;
        public bool  IsFull       => stock >= max_stock;

        public override string ToString() => $"{name} | Stock: {stock}/{max_stock} | Price: ₹{price}";
    }
    

    [Serializable]
    public class InventoryUpdate
    {
        public string item_key;
        public int    stock;   
        public float  price;   

        public InventoryUpdate(string itemKey, int newStock, float newPrice)
        {
            item_key = itemKey;
            stock    = newStock;
            price    = newPrice;
        }
    }

    [Serializable]
    public class PriceUpdate
    {
        public float price_multiplier;
        public PriceUpdate(float multiplier) { price_multiplier = multiplier; }
    }

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
