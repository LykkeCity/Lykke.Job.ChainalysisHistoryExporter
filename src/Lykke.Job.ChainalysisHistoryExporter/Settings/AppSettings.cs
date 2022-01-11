using Lykke.Sdk.Settings;

namespace Lykke.Job.ChainalysisHistoryExporter.Settings
{
    public class AppSettings : BaseAppSettings
    {
        public EmailSettings Email { get; set; }
        public AzureStorageSettings AzureStorage { get; set; }
        public MongoStorageSettings MongoStorage { get; set; }
        public DepositWalletsProvidersSettings DepositWalletsProviders { get; set; }
        public DepositsHistoryProvidersSettings DepositsHistoryProviders { get; set; }
        public WithdrawalsHistoryProvidersSettings WithdrawalsHistoryProviders { get; set; }
        public SlackSettings Slack { get; set; }
        public ReportSettings Report { get; set; }
        public ScheduleSettings Schedule { get; set; }
        public AssetsClientSettings Assets { get; set; }
        public BtcSettings Btc { get; set; }
        public EthSettings Eth { get; set; }
        public XrpSettings Xrp { get; set; }
    }
}
