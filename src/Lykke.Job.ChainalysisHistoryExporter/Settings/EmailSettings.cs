using System.Collections.Generic;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.ChainalysisHistoryExporter.Settings
{
    public class EmailSettings
    {
        public string SmtpHost { get; set; }
        [Optional]
        public int Port { get; set; }
        public bool EnableSsl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FromDisplayName { get; set; }
        public string FromEmailAddress { get; set; }
        public IReadOnlyCollection<string> To { get; set; }
        public IReadOnlyCollection<string> Bcc { get; set; }
    }
}
