using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.ChainalysisHistoryExporter.Settings
{
    public class SlackSettings
    {
        [Optional]
        public string ReportChannel { get; set; }
        
        [Optional]
        public string AuthToken { get; set; }
    }
}
