using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.InsightApi
{
    public class InsightApiTransaction
    {
        // ReSharper disable once StringLiteralTypo
        [JsonProperty("txid")]
        public string Id { get; set; }

        [JsonProperty("vin")]
        public IReadOnlyCollection<InsightApiTransactionInput> Inputs { get; set; }
    }
}
