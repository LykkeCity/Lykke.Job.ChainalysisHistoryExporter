using Newtonsoft.Json;

namespace Lykke.Tools.ChainalysisHistoryExporter.InsightApi
{
    public class InsightApiTransactionInput
    {
        // ReSharper disable once StringLiteralTypo
        [JsonProperty("addr")]
        public string Address { get; set; }
    }
}