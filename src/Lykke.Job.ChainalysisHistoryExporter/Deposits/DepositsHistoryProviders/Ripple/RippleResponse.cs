using System;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.Ripple
{
    public class RippleResponse<TResult> where TResult : RippleResult
    {
        [JsonProperty("result")]
        public TResult Result { get; set; }
    }
}
