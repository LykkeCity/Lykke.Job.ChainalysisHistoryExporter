namespace Lykke.Job.ChainalysisHistoryExporter.Configuration
{
    public class ReportSettings
    {
        public string TransactionsFilePath { get; set; }
        public string DepositWalletsFilePath { get; set; }
        public string AzureStorageConnString { get; set; }
    }
}
