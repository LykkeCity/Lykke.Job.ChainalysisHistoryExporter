using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.ChainalysisHistoryExporter.Settings
{
    public class MongoStorageSettings
    {
        [MongoCheck]
        [Optional]
        public string BlockchainWalletsConnString { get; set; }
        
        [Optional]
        public string BlockchainWalletsDbName { get; set; }

        [MongoCheck]
        public string HangFireConnString { get; set; }
    }
}
