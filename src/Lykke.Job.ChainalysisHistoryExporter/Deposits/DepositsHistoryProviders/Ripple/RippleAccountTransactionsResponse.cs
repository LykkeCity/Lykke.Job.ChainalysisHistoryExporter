using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.Ripple
{
    public class RippleAccountTransactionsResponse : RippleResponse<RippleAccountTransactionsResponse.Value>
    {
        public class Value : RippleResult
        {
            [JsonProperty("account")]
            public string Account { get; set; }

            [JsonProperty("ledger_index_min")]
            public long LedgerIndexMin { get; set; }

            [JsonProperty("ledger_index_max")]
            public long LedgerIndexMax { get; set; }

            [JsonProperty("marker")]
            public object Marker { get; set; }

            [JsonProperty("transactions")]
            public RippleTransaction[] Transactions { get; set; }

            [JsonProperty("validated")]
            public bool Validated { get; set; }
        }
    }
}
