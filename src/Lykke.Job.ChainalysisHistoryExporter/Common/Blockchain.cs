using System.Collections.Generic;

namespace Lykke.Job.ChainalysisHistoryExporter.Common
{
    public class Blockchain
    {
        public string CryptoCurrency { get; set; }
        public string BilId { get; set; }
        public string AssetBlockchain { get; set; }
        public IReadOnlyCollection<string> AssetReferences { get; set; }
    }
}
