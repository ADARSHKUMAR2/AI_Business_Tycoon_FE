using System;
using System.Collections.Generic;

namespace AIBusinessTycoon.Data
{
    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string player_id;
        public string display_name;
        public float value;
    }

    [Serializable]
    public class LeaderboardUpdateEvent
    {
        public string type; // "leaderboard.updated"
        public string event_id;
        public string franchise_name;
        public int time_remaining_seconds;
        public List<LeaderboardEntry> entries;
    }

    [Serializable]
    public class RealtimeSubscribeMessage
    {
        public string type = "subscribe";
        public string[] topics = { "leaderboard" };
    }
}
