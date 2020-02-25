using System;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.Ripple
{
    public class RippleTransaction
    {
        [JsonProperty("meta")]
        public Metadata Meta { get; set; }

        [JsonProperty("tx")]
        public Body Tx { get; set; }

        [JsonProperty("validated")]
        public bool Validated { get; set; }

        public class Metadata
        {
            [JsonProperty("delivered_amount")]
            public object DeliveredAmount { get; set; }

            public string TransactionResult { get; set; }
        }

        public class Body
        {
            [JsonProperty("hash")]
            public string Hash { get; set; }

            public string Account { get; set; }
            public string Fee { get; set; }
            public string TransactionType { get; set; }
            public string Destination { get; set; }
            public uint? DestinationTag { get; set; }
            public object Amount { get; set; }
        }
    }
}
