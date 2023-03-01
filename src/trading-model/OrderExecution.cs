using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trading_model
{
    public class OrderExecution
    {
        public string id { get; set; }

        public string orderId { get; set; }
        public string customerId { get; set; }

        public int quantity { get; set; }
        public string symbol { get; set; }
        public string action { get; set; }

        public decimal price { get; set; }

        public string type { get { return "orderExecution"; } }

        public DateTime executedAt { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? ttl { get; set; }
    }
}
