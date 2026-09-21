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
}
