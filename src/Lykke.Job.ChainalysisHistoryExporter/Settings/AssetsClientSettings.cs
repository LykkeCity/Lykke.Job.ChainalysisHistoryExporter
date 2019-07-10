using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.ChainalysisHistoryExporter.Settings
{
    public class AssetsClientSettings
    {
        [HttpCheck("/api/isalive")]
        public string ServiceUrl { get; set; }
    }
}
