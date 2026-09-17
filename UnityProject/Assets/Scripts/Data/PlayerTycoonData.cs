using System;
using System.Collections.Generic;

namespace AIBusinessTycoon.Data
{
    [Serializable]
    public class Position
    {
        public int x;
        public int y;
    }

    [Serializable]
    public class PlayerStats
    {
        public float total_revenue;
        public float total_expenses;
        public int businesses_owned;
        public int employees_hired;
        public int land_tiles_owned;
        public int level;
        public int experience;
    }

    [Serializable]
    public class PlayerTycoonData
    {
        public string player_id;
        public string name;
        public float money;
        public PlayerStats stats;
        public string created_at;
        public string last_login;
        public string last_updated;
        
        // Simplified properties for easy access
        public string PlayerId => player_id;
        public string PlayerName => name;
        public float CurrentMoney => money;
        public int Level => stats?.level ?? 1;
        public int OwnedBusinessCount => stats?.businesses_owned ?? 0;
        
        public PlayerTycoonData()
        {
            player_id = "";
            name = "Guest";
            money = 10000f;
            stats = new PlayerStats { level = 1, businesses_owned = 0 };
        }
        
        public override string ToString()
        {
            return $"Player: {name} (ID: {player_id}), Money: ₹{money}, Level: {Level}, Businesses: {OwnedBusinessCount}";
        }
    }
}
