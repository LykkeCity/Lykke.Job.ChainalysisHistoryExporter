using Lykke.Job.ChainalysisHistoryExporter.Settings.JobSettings;
using Lykke.Sdk.Settings;

namespace Lykke.Job.ChainalysisHistoryExporter.Settings
{
    public class AppSettings : BaseAppSettings
    {
        public ChainalysisHistoryExporterJobSettings ChainalysisHistoryExporterJob { get; set; }
    }
}
