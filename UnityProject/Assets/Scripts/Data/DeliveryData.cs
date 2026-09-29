using System;

namespace AIBusinessTycoon.Data
{
    [Serializable]
    public class DeliveryScheduleData
    {
        public int delivery_interval_minutes = 5;
        public string last_delivery_at;
        public string next_delivery_at;
        public int supply_window_seconds = 90;
        public float express_delivery_cost = 500f;
        public bool is_active = true;
    }

    [Serializable]
    public class ExpressDeliveryRequest
    {
        public string idempotency_key;

        public ExpressDeliveryRequest(string idempotencyKey)
        {
            idempotency_key = idempotencyKey;
        }
    }

    [Serializable]
    public class DeliveryStatusResponse
    {
        public string business_id;
        public bool supply_available;
        public int seconds_until_next;
        public int supply_window_remaining;
        public string last_delivery_at;
        public string next_delivery_at;
        public int delivery_interval_minutes;
        public float express_delivery_cost;
    }

    [Serializable]
    public class WorldStateResponse
    {
        public string time_of_day;
        public string weather;
        public string state_started_at;
        public string next_time_change_at;
        public string next_weather_change_at;
        public float rain_spawn_multiplier;
        public float night_spawn_multiplier;
    }
}
