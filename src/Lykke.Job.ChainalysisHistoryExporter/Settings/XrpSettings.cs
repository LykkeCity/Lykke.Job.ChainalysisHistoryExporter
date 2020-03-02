using System;

namespace Lykke.Job.ChainalysisHistoryExporter.Settings
{
    public class XrpSettings
    {
        public string RpcUrl { get; set; }
        public TimeSpan CacheExpirationPeriod { get; set; }
    }
}
