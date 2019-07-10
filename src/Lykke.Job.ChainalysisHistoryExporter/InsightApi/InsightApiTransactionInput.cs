using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.InsightApi
{
    public class InsightApiTransactionInput
    {
        // ReSharper disable once StringLiteralTypo
        [JsonProperty("addr")]
        public string Address { get; set; }
    }
}