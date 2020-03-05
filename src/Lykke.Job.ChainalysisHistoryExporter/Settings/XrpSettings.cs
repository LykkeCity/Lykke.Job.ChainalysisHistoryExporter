using System;

namespace Lykke.Job.ChainalysisHistoryExporter.Settings
{
    public class XrpSettings
    {
        public string RpcUrl { get; set; }
        public string RpcUsername { get; set; }
        public string RpcPassword { get; set; }
        public TimeSpan CacheExpirationPeriod { get; set; }
    }
}
