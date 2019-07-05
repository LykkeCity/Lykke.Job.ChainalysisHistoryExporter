using Lykke.Sdk.Settings;

namespace Lykke.Job.ChainalysisHistoryExporter.Settings
{
    public class AppSettings : BaseAppSettings
    {
        public AzureStorageSettings AzureStorage { get; set; }
        public MongoStorageSettings MongoStorage { get; set; }
        public DepositWalletProvidersSettings DepositWalletProviders { get; set; }
        public DepositHistoryProvidersSettings DepositHistoryProviders { get; set; }
        public WithdrawalHistoryProvidersSettings WithdrawalHistoryProviders { get; set; }
        public ReportSettings Report { get; set; }
        public AssetsClientSettings Assets { get; set; }
        public BtcSettings Btc { get; set; }
        public EthSettings Eth { get; set; }
        public LtcSettings Ltc { get; set; }
        public BchSettings Bch { get; set; }
    }
}
