using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;
using Microsoft.Extensions.Logging;
using Polly;

namespace Lykke.Tools.ChainalysisHistoryExporter.Deposits
{
    internal class DepositsExporter : IDisposable
    {
        private readonly ILogger<DepositsExporter> _logger;
        private readonly Report _report;
        private readonly IEnumerable<IDepositWalletsProvider> _depositWalletsProviders;
        private readonly IDepositsHistoryProvider _depositsHistoryProvider;
        private readonly SemaphoreSlim _concurrencySemaphore;

        public DepositsExporter(
            ILogger<DepositsExporter> logger,
            Report report,
            IEnumerable<IDepositWalletsProvider> depositWalletsProviders,
            IDepositsHistoryProvider depositsHistoryProvider)
        {
            _logger = logger;
            _report = report;
            _depositWalletsProviders = depositWalletsProviders;
            _depositsHistoryProvider = depositsHistoryProvider;

            _concurrencySemaphore = new SemaphoreSlim(8);
        }

        public async Task ExportAsync()
        {
            var depositWallets = await LoadDepositWalletsAsync();
            var tasks = new List<Task>(512);

            _logger.LogInformation("Exporting deposits...");

            foreach (var wallet in depositWallets)
            {
                await _concurrencySemaphore.WaitAsync();

                tasks.Add(ProcessDepositWalletAsync(wallet));

                if (tasks.Count >= 500)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }

            _logger.LogInformation("Deposits exporting done");
        }

        public void Dispose()
        {
            _concurrencySemaphore?.Dispose();
        }

        private async Task<IReadOnlyCollection<DepositWallet>> LoadDepositWalletsAsync()
        {
            _logger.LogInformation("Loading deposit wallets...");

            var allWallets = new HashSet<DepositWallet>(131072); // 2 ^ 17

            foreach (var provider in _depositWalletsProviders)
            {
                PaginatedList<DepositWallet> wallets = null;
                var batchNumber = 1;

                do
                {
                    _logger.LogInformation($"Loading deposit wallets batch {batchNumber} using {provider.GetType().Name}");

                    wallets = await Policy
                        .Handle<Exception>(ex =>
                        {
                            _logger.LogWarning(ex, $"Failed to get deposits wallets using {provider.GetType().Name}. Operation will be retried.");
                            return true;
                        })
                        .WaitAndRetryForeverAsync(i => TimeSpan.FromSeconds(Math.Min(i, 5)))
                        .ExecuteAsync(async () => await provider.GetWalletsAsync(wallets?.Continuation));

                    foreach (var wallet in wallets.Items)
                    {
                        allWallets.Add(wallet);
                    }

                    batchNumber++;
                } while (wallets.Continuation != null);
            }

            _logger.LogInformation($"Deposit wallets loading done. {allWallets.Count} loaded");

            return allWallets;
        }

        private async Task ProcessDepositWalletAsync(DepositWallet wallet)
        {
            try
            {
                PaginatedList<Transaction> transactions = null;
                var batchNumber = 1;

                do
                {
                    if (batchNumber % 5 == 0)
                    {
                        _logger.LogInformation($"Exporting deposit wallet {wallet.CryptoCurrency}:{wallet.Address} batch {batchNumber}");
                    }

                    transactions = await Policy
                        .Handle<Exception>(ex =>
                        {
                            _logger.LogWarning(ex, $"Failed to get deposits history of {wallet.CryptoCurrency}:{wallet.Address}. Operation will be retried.");
                            return true;
                        })
                        .WaitAndRetryForeverAsync(i => TimeSpan.FromSeconds(Math.Min(i, 5)))
                        .ExecuteAsync(async () => await _depositsHistoryProvider.GetHistoryAsync(wallet, transactions?.Continuation));

                    foreach (var tx in transactions.Items)
                    {
                        await _report.AddTransactionAsync(tx);
                    }

                    batchNumber++;

                } while (transactions.Continuation != null);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to process deposit wallet: {wallet.CryptoCurrency}:{wallet.Address}:{wallet.UserId}", ex);
            }
            finally
            {
                _concurrencySemaphore.Release();
            }
        }
    }
}
