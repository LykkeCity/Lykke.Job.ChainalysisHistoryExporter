namespace Lykke.Tools.ChainalysisHistoryExporter.Configuration
{
    public class AzureStorageSettings
    {
        public string CashoutProcessorConnString { get; set; }
        public string OperationsExecutorConnString { get; set; }
        public string BlockchainWalletsConnString { get; set; }
        public string ClientPersonalInfoConnString { get; set; }
        public string BlockchainWalletsTable { get; set; }
    }
}
