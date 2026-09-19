using System;
using System.Collections.Generic;

namespace AIBusinessTycoon.Data
{
    [Serializable]
    public enum BusinessType { kirana, pizza, cafe }

    [Serializable]
    public class BusinessStats
    {
        public float total_revenue          = 0f;
        public float total_expenses         = 0f;
        public int   total_customers_served = 0;
        public float average_transaction    = 0f;

        public float CalculateProfit()       => total_revenue - total_expenses;
        public float CalculateProfitMargin() => total_revenue == 0 ? 0f
                                                : (CalculateProfit() / total_revenue) * 100f;
    }

    [Serializable]
    public class BusinessCreateRequest
    {
        public string player_id;
        public string business_type;
        public string name;
        public int    position_x;
        public int    position_y;

        public BusinessCreateRequest(string playerId, string businessType,
                                     string businessName, int posX, int posY)
        {
            player_id     = playerId;
            business_type = businessType;
            name          = businessName;
            position_x    = posX;
            position_y    = posY;
        }
    }

    /// <summary>
    /// Complete business model with full state.
    /// Mirrors backend Business model.
    /// Phase 3: Added trash_items and store_rating.
    /// </summary>
    [Serializable]
    public class BusinessData
    {
        public string              business_id;
        public string              player_id;
        public string              business_type;
        public string              name;
        public int                 position_x;
        public int                 position_y;
        public float               price_multiplier = 1.0f;
        public bool                is_open          = true;
        public List<Employee>      employees        = new List<Employee>();
        public List<TrashItem>     trash_items      = new List<TrashItem>(); // Phase 3: NEW
        public float               store_rating     = 5.0f;                  // Phase 3: NEW (0.0–5.0)
        public BusinessStats       stats;
        public string              created_at;
        public string              last_updated;

        // inventory is a Dictionary — JsonUtility cannot deserialize it.
        // Use GetRequestRaw + manual parsing if you need inventory data in Unity.

        public Position GridPosition => new Position { x = position_x, y = position_y };
        public BusinessType BusinessTypeEnum
        {
            get
            {
                if (Enum.TryParse<BusinessType>(business_type, out var result)) return result;
                return BusinessType.kirana;
            }
        }

        // Phase 3: convenience helpers
        public int   TrashCount  => trash_items?.Count ?? 0;
        public bool  IsClean     => store_rating >= 4.5f;
        public string RatingText => $"⭐ {store_rating:F1}/5.0";

        public BusinessData() { stats = new BusinessStats(); }

        public override string ToString()
        {
            return $"{name} ({business_type}) | Revenue: ₹{stats.total_revenue} | Rating: {store_rating:F1} | Open: {is_open}";
        }
    }

    [Serializable]
    public class BusinessListResponse
    {
        public bool              success;
        public string            message;
        public List<BusinessData> businesses;
    }
}
