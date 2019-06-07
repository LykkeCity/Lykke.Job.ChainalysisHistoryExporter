using System;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Assets;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Lykke.Tools.ChainalysisHistoryExporter.Deposits;
using Lykke.Tools.ChainalysisHistoryExporter.Deposits.DepositHistoryProviders.Bitcoin;
using Lykke.Tools.ChainalysisHistoryExporter.Deposits.DepositHistoryProviders.BitcoinCash;
using Lykke.Tools.ChainalysisHistoryExporter.Deposits.DepositHistoryProviders.Ethereum;
using Lykke.Tools.ChainalysisHistoryExporter.Deposits.DepositHistoryProviders.LiteCoin;
using Lykke.Tools.ChainalysisHistoryExporter.Deposits.DepositWalletsProviders;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;
using Lykke.Tools.ChainalysisHistoryExporter.Withdrawals;
using Lykke.Tools.ChainalysisHistoryExporter.Withdrawals.WithdrawalHistoryProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lykke.Tools.ChainalysisHistoryExporter
{
    public class Program : IDisposable
    {
        private IServiceProvider ServiceProvider { get; }

        private Program()
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            var configuration = configurationBuilder.Build();

            var services = new ServiceCollection();

            services.AddSingleton<Report>();
            services.AddSingleton<Exporter>();
            services.AddSingleton<BlockchainsProvider>();
            services.AddSingleton<AssetsClient>();
            services.AddTransient<WithdrawalsExporter>();
            services.AddTransient<DepositsExporter>();
            
            services.AddTransient<IWithdrawalsHistoryProvider, BilCashoutWithdrawalsHistoryProvider>();
            services.AddTransient<IWithdrawalsHistoryProvider, BilCashoutsBatchWithdrawalsHistoryProvider>();
            //services.AddTransient<IWithdrawalsHistoryProvider, HistoryServiceWithdrawalsHistoryProvider>();
            
            services.AddTransient<IDepositWalletsProvider, BilAzureDepositWalletsProvider>();
            services.AddTransient<IDepositWalletsProvider, BilMongoDepositWalletsProvider>();
            services.AddTransient<IDepositWalletsProvider, BcnCredentialsDepositWalletsProvider>();
            services.AddTransient<IDepositWalletsProvider, WalletCredentialsDepositWalletsProvider>();

            services.AddTransient<IDepositsHistoryProvider, BtcDepositsHistoryProvider>();
            services.AddTransient<IDepositsHistoryProvider, EthDepositsHistoryProvider>();
            services.AddTransient<IDepositsHistoryProvider, LtcDepositsHistoryProvider>();
            services.AddTransient<IDepositsHistoryProvider, BchDepositsHistoryProvider>();
            
            services.AddLogging(logging =>
            {
                logging.AddConsole();
            });

            services.Configure<AzureStorageSettings>(configuration.GetSection("AzureStorage"));
            services.Configure<MongoStorageSettings>(configuration.GetSection("MongoStorage"));
            services.Configure<ReportSettings>(configuration.GetSection("Report"));
            services.Configure<ServicesSettings>(configuration.GetSection("Services"));
            services.Configure<DepositWalletProvidersSettings>(configuration.GetSection("DepositWalletProviders"));
            services.Configure<DepositHistoryProvidersSettings>(configuration.GetSection("DepositHistoryProviders"));
            services.Configure<WithdrawalHistoryProvidersSettings>(configuration.GetSection("WithdrawalHistoryProviders"));
            services.Configure<BtcSettings>(configuration.GetSection("Btc"));
            services.Configure<EthSettings>(configuration.GetSection("Eth"));
            services.Configure<LtcSettings>(configuration.GetSection("Ltc"));
            services.Configure<BchSettings>(configuration.GetSection("Bch"));

            ServiceProvider = services.BuildServiceProvider();
        }

        private static async Task Main(string[] args)
        {
            using (var program = new Program())
            {
                await program.RunAsync();
            }
        }

        private async Task RunAsync()
        {
            var exporter = ServiceProvider.GetRequiredService<Exporter>();
            var assetsProvider = ServiceProvider.GetRequiredService<AssetsClient>();

            await assetsProvider.InitializeAsync();

            await exporter.ExportAsync();
        }

        public void Dispose()
        {
            (ServiceProvider as IDisposable)?.Dispose();
        }
    }
}
