using System.Collections.Generic;

namespace Lykke.Tools.ChainalysisHistoryExporter.Configuration
{
    internal class WithdrawalHistoryProvidersSettings
    {
        public IReadOnlyCollection<string> Providers { get; set; }
    }
}
