using System;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Assets;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Lykke.Tools.ChainalysisHistoryExporter.Deposits;
using Lykke.Tools.ChainalysisHistoryExporter.Deposits.DepositHistoryProviders.Bitcoin;
using Lykke.Tools.ChainalysisHistoryExporter.Deposits.DepositWalletsProviders;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;
using Lykke.Tools.ChainalysisHistoryExporter.Withdrawals;
using Lykke.Tools.ChainalysisHistoryExporter.Withdrawals.WithdrawalHistoryProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lykke.Tools.ChainalysisHistoryExporter
{
    internal class Program
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
            services.AddSingleton<WithdrawalsExporter>();
            services.AddSingleton<DepositsExporter>();
            services.AddSingleton<BlockchainsProvider>();
            services.AddSingleton<AssetsProvider>();

            services.AddSingleton<IWithdrawalsHistoryProvider, BilCashoutWithdrawalsHistoryProvider>();
            services.AddSingleton<IWithdrawalsHistoryProvider, BilCashoutsBatchWithdrawalsHistoryProvider>();
            
            services.AddSingleton<IDepositWalletsProvider, BilDepositWalletsProvider>();
            services.AddSingleton<IDepositWalletsProvider, BcnCredentialsDepositWalletsProvider>();
            services.AddSingleton<IDepositWalletsProvider, WalletCredentialsDepositWalletsProvider>();

            services.AddSingleton<IDepositsHistoryProvider, BtcDepositsHistoryProvider>();
            
            services.AddLogging(logging =>
            {
                logging.AddConsole();
            });

            services.Configure<AzureStorageSettings>(configuration.GetSection("AzureStorage"));
            services.Configure<ReportSettings>(configuration.GetSection("Report"));
            services.Configure<ServicesSettings>(configuration.GetSection("Services"));
            services.Configure<BtcSettings>(configuration.GetSection("Btc"));

            ServiceProvider = services.BuildServiceProvider();
        }

        private static async Task Main(string[] args)
        {
            var program = new Program();

            await program.RunAsync();
        }

        private async Task RunAsync()
        {
            var exporter = ServiceProvider.GetRequiredService<Exporter>();
            var assetsProvider = ServiceProvider.GetRequiredService<AssetsProvider>();

            await assetsProvider.InitializeAsync();

            await exporter.ExportAsync();
        }
    }
}
