using System;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.Ripple
{
    public class RippleAccountTransactionsRequest : RippleRequest<RippleAccountTransactionsRequest.Parameters>
    {
        public RippleAccountTransactionsRequest(string account, long ledgerIndexMin = -1, long ledgerIndexMax = -1, object marker = null)
            : base("account_tx", new Parameters(account, ledgerIndexMin, ledgerIndexMax, marker))
        {
        }

        public class Parameters
        {
            public Parameters(string account, long ledgerIndexMin = -1, long ledgerIndexMax = -1, object marker = null)
            {
                Account = account ?? throw new ArgumentNullException(nameof(account));
                LedgerIndexMin = ledgerIndexMin;
                LedgerIndexMax = ledgerIndexMax;
                Marker = marker;
            }

            [JsonProperty("account")]
            public string Account { get; }

            [JsonProperty("ledger_index_min")]
            public long LedgerIndexMin { get; }

            [JsonProperty("ledger_index_max")]
            public long LedgerIndexMax { get; }

            [JsonProperty("forward")]
            public bool Forward { get; } = true;

            [JsonProperty("marker")]
            public object Marker { get; }
        }
    }
}
