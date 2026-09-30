using System;

namespace AIBusinessTycoon.Data
{
    /// <summary>
    /// Represents an active franchise tournament event.
    /// Matches the EventResponse model from the backend API.
    /// </summary>
    [Serializable]
    public class EventResponse
    {
        public string event_id;
        public string franchise_name;
        public string status;              // "upcoming" | "active" | "completed"
        public string start_time;          // ISO 8601 format
        public string end_time;            // ISO 8601 format
        public float entry_fee;
        public int max_winners;
        public bool is_registered;
        public int participant_count;
        
        /// <summary>
        /// Returns true if the event is currently active.
        /// </summary>
        public bool IsActive => status == "active";
        
        /// <summary>
        /// Returns true if the event is upcoming (not started yet).
        /// </summary>
        public bool IsUpcoming => status == "upcoming";
        
        /// <summary>
        /// Returns true if the player can join this event.
        /// </summary>
        public bool CanJoin => (IsActive || IsUpcoming) && !is_registered;
        
        /// <summary>
        /// Parse the end_time string to DateTime.
        /// </summary>
        public DateTime GetEndTime()
        {
            if (DateTime.TryParse(end_time, out DateTime result))
                return result;
            return DateTime.UtcNow;
        }
        
        /// <summary>
        /// Calculate remaining seconds until event ends.
        /// </summary>
        public int GetRemainingSeconds()
        {
            DateTime endTime = GetEndTime();
            TimeSpan remaining = endTime - DateTime.UtcNow;
            return Math.Max(0, (int)remaining.TotalSeconds);
        }
        
        /// <summary>
        /// Format remaining time as "MM:SS".
        /// </summary>
        public string GetFormattedTimeRemaining()
        {
            int totalSeconds = GetRemainingSeconds();
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}
