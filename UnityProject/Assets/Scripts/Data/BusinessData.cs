using System;
using System.Collections.Generic;

namespace AIBusinessTycoon.Data
{
    /// <summary>
    /// Business types available in the game.
    /// Mirrors backend BusinessType enum.
    /// </summary>
    [Serializable]
    public enum BusinessType
    {
        kirana,
        pizza,
        cafe
    }

    /// <summary>
    /// Business performance statistics.
    /// Mirrors backend BusinessStats model.
    /// </summary>
    [Serializable]
    public class BusinessStats
    {
        public float total_revenue = 0f;
        public float total_expenses = 0f;
        public int total_customers_served = 0;
        public float average_transaction = 0f;

        public float CalculateProfit()
        {
            return total_revenue - total_expenses;
        }

        public float CalculateProfitMargin()
        {
            if (total_revenue == 0) return 0f;
            return (CalculateProfit() / total_revenue) * 100f;
        }
    }

    /// <summary>
    /// Request payload for creating a new business.
    /// Mirrors backend BusinessCreate schema.
    /// </summary>
    [Serializable]
    public class BusinessCreateRequest
    {
        public string player_id;
        public string business_type;
        public string name;
        public int position_x;
        public int position_y;

        public BusinessCreateRequest(string playerId, string businessType, string businessName, int posX, int posY)
        {
            player_id = playerId;
            business_type = businessType;
            name = businessName;
            position_x = posX;
            position_y = posY;
        }
    }

    /// <summary>
    /// Complete business model with full state.
    /// Mirrors backend Business model.
    /// </summary>
    [Serializable]
    public class BusinessData
    {
        public string business_id;
        public string player_id;
        public string business_type;
        public string name;
        public int position_x;
        public int position_y;
        public float price_multiplier = 1.0f;
        public bool is_open = true;
        public BusinessStats stats;
        public string created_at;
        public string last_updated;

        // Convenience properties
        public Position GridPosition => new Position { x = position_x, y = position_y };
        public BusinessType BusinessTypeEnum
        {
            get
            {
                if (Enum.TryParse<BusinessType>(business_type, out var result))
                    return result;
                return BusinessType.kirana;
            }
        }

        public BusinessData()
        {
            stats = new BusinessStats();
        }

        public override string ToString()
        {
            return $"{name} ({business_type}) at ({position_x}, {position_y}) | Revenue: ₹{stats.total_revenue} | Open: {is_open}";
        }
    }

    /// <summary>
    /// Response wrapper for business list.
    /// </summary>
    [Serializable]
    public class BusinessListResponse
    {
        public bool success;
        public string message;
        public List<BusinessData> businesses;
    }
}
