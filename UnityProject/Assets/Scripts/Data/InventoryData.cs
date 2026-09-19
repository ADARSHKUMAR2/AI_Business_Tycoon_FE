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

    /// <summary>
    /// ZERO-DEPENDENCY JSON TRICK:
    /// Unity's JsonUtility cannot parse Dictionaries. Because we know all the item keys
    /// from our Python backend settings, we explicitly declare them here as fields.
    /// JsonUtility will automatically drop the data into the correct slots!
    /// </summary>
    [Serializable]
    public class InventoryDict
    {
        // ── Kirana Store ──
        public InventoryItem rice;
        public InventoryItem dal;
        public InventoryItem milk;
        public InventoryItem bread;
        public InventoryItem oil;
        public InventoryItem sugar;
        public InventoryItem salt;
        public InventoryItem tea;
        
        // ── Pizza Outlet ──
        public InventoryItem margherita;
        public InventoryItem pepperoni;
        public InventoryItem garlic_bread;
        public InventoryItem cola;
        
        // ── Cafe ──
        public InventoryItem espresso;
        public InventoryItem cappuccino;
        public InventoryItem croissant;
        public InventoryItem muffin;

        /// <summary>
        /// Converts the JSON slots back into an iterable list for UI and gameplay logic.
        /// Returns KeyValuePairs so you know the backend key (e.g., "rice") to use when upgrading!
        /// </summary>
        public List<KeyValuePair<string, InventoryItem>> GetActiveItems()
        {
            var list = new List<KeyValuePair<string, InventoryItem>>();
            
            if (rice != null) list.Add(new KeyValuePair<string, InventoryItem>("rice", rice));
            if (dal != null) list.Add(new KeyValuePair<string, InventoryItem>("dal", dal));
            if (milk != null) list.Add(new KeyValuePair<string, InventoryItem>("milk", milk));
            if (bread != null) list.Add(new KeyValuePair<string, InventoryItem>("bread", bread));
            if (oil != null) list.Add(new KeyValuePair<string, InventoryItem>("oil", oil));
            if (sugar != null) list.Add(new KeyValuePair<string, InventoryItem>("sugar", sugar));
            if (salt != null) list.Add(new KeyValuePair<string, InventoryItem>("salt", salt));
            if (tea != null) list.Add(new KeyValuePair<string, InventoryItem>("tea", tea));
            
            if (margherita != null) list.Add(new KeyValuePair<string, InventoryItem>("margherita", margherita));
            if (pepperoni != null) list.Add(new KeyValuePair<string, InventoryItem>("pepperoni", pepperoni));
            if (garlic_bread != null) list.Add(new KeyValuePair<string, InventoryItem>("garlic_bread", garlic_bread));
            if (cola != null) list.Add(new KeyValuePair<string, InventoryItem>("cola", cola));
            
            if (espresso != null) list.Add(new KeyValuePair<string, InventoryItem>("espresso", espresso));
            if (cappuccino != null) list.Add(new KeyValuePair<string, InventoryItem>("cappuccino", cappuccino));
            if (croissant != null) list.Add(new KeyValuePair<string, InventoryItem>("croissant", croissant));
            if (muffin != null) list.Add(new KeyValuePair<string, InventoryItem>("muffin", muffin));
            
            return list;
        }
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
