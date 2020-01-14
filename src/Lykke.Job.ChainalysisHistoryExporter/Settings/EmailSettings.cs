using System.Collections.Generic;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.ChainalysisHistoryExporter.Settings
{
    public class EmailSettings
    {
        [HttpCheck("/api/isalive")]
        public string EmailSenderServiceUrl { get; set; }

        public string To { get; set; }

        public IReadOnlyCollection<string> Bcc { get; set; }
    }
}
