using System.Collections.Generic;

namespace Lykke.Job.ChainalysisHistoryExporter.Configuration
{
    public class WithdrawalHistoryProvidersSettings
    {
        public IReadOnlyCollection<string> Providers { get; set; }
    }
}
