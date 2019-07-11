using Lykke.SettingsReader.Attributes;

namespace Lykke.Job.ChainalysisHistoryExporter.Settings
{
    public class AzureStorageSettings
    {
        [AzureTableCheck]
        public string LogsConnString { get; set; }

        [AzureBlobCheck]
        public string ReportStorageConnString { get; set; }

        [AzureTableCheck]
        public string CashOperationsConnString { get; set; }
        
        [AzureTableCheck]
        public string CashoutProcessorConnString { get; set; }
        
        [AzureTableCheck]
        public string OperationsExecutorConnString { get; set; }
        
        [AzureTableCheck]
        public string BlockchainWalletsConnString { get; set; }
        
        [AzureTableCheck]
        public string ClientPersonalInfoConnString { get; set; }
        
        [Optional]
        public string BlockchainWalletsTable { get; set; } = "BlockchainWallets";
    }
}
