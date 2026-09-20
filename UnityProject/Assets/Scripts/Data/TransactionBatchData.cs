using System;
using System.Collections.Generic;

namespace AIBusinessTycoon.Data
{
    [Serializable]
    public class TransactionBatchData
    {
        public Dictionary<string, int> items_sold = new Dictionary<string, int>();
        public float total_revenue = 0f;
        public int total_customers_served = 0;
    }
}
