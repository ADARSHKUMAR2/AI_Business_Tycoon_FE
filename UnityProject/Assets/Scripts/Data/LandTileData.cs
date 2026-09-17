using System;
using System.Collections.Generic;

namespace AIBusinessTycoon.Data
{
    /// <summary>
    /// Types of land tiles.
    /// Mirrors backend LandType enum.
    /// </summary>
    [Serializable]
    public enum LandType
    {
        empty,
        shop,
        road,
        parking
    }

    /// <summary>
    /// Represents a single land tile.
    /// Mirrors backend LandTile model.
    /// </summary>
    [Serializable]
    public class LandTile
    {
        public Position position;
        public string tile_type;
        public float purchase_cost;
        public string purchased_at;
        public string business_id;

        // Convenience properties
        public LandType TileTypeEnum
        {
            get
            {
                if (Enum.TryParse<LandType>(tile_type, out var result))
                    return result;
                return LandType.empty;
            }
        }

        public bool IsEmpty => string.IsNullOrEmpty(business_id) && tile_type == "empty";
        public bool HasBusiness => !string.IsNullOrEmpty(business_id);

        public LandTile()
        {
            position = new Position();
            tile_type = "empty";
        }

        public override string ToString()
        {
            return $"Tile at ({position.x}, {position.y}) | Type: {tile_type} | Cost: ₹{purchase_cost}";
        }
    }

    /// <summary>
    /// Request to purchase a land tile.
    /// Mirrors backend LandPurchaseRequest schema.
    /// </summary>
    [Serializable]
    public class LandPurchaseRequest
    {
        public string player_id;
        public Position position;

        public LandPurchaseRequest(string playerId, Position pos)
        {
            player_id = playerId;
            position = pos;
        }
    }

    /// <summary>
    /// Response for available land tiles.
    /// </summary>
    [Serializable]
    public class AvailableLandResponse
    {
        public Position position;
        public float cost;
        public bool adjacent;
    }
}
