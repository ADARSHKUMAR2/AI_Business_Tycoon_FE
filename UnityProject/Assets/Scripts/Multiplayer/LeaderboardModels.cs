using System;
using System.Collections.Generic;

namespace AIBusinessTycoon.Multiplayer
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
        public string type; // "event.leaderboard.updated"
        public string event_id;
        public string franchise_name;
        public int time_remaining_seconds;
        public List<LeaderboardEntry> entries;
    }

    [Serializable]
    internal class RealtimeSubscribeMessage
    {
        public string type = "subscribe";
        public string[] topics = { "leaderboard" };
    }
}
