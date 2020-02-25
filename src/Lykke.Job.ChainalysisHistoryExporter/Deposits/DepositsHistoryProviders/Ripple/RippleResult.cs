using System;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.Ripple
{
    /// <summary>
    /// Base JSON-RPC result.
    /// </summary>
    public abstract class RippleResult
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }
    }
}
