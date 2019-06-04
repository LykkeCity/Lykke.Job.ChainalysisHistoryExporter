using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        private readonly IEnumerable<IDepositsHistoryProvider> _depositsHistoryProviders;
        private readonly SemaphoreSlim _concurrencySemaphore;
        private int _processedWalletsCount;
        private int _exportedDepositsCount;
        private int _totalDepositWalletsCount;

        public DepositsExporter(
            ILogger<DepositsExporter> logger,
            Report report,
            IEnumerable<IDepositWalletsProvider> depositWalletsProviders,
            IEnumerable<IDepositsHistoryProvider> depositsHistoryProviders)
        {
            _logger = logger;
            _report = report;
            _depositWalletsProviders = depositWalletsProviders;
            _depositsHistoryProviders = depositsHistoryProviders;

            _concurrencySemaphore = new SemaphoreSlim(8);
        }

        public async Task ExportAsync()
        {
            var depositWallets = await LoadDepositWalletsAsync();

            _totalDepositWalletsCount = depositWallets.Count;

            await SaveDepositWalletsAsync(depositWallets);

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

            _logger.LogInformation($"Deposits exporting done. {_processedWalletsCount} deposit wallets processed. {_exportedDepositsCount} deposits exported");
        }

        private static async Task SaveDepositWalletsAsync(IReadOnlyCollection<DepositWallet> depositWallets)
        {
            var stream = File.Open("deposit-wallets.csv", FileMode.Create, FileAccess.Write, FileShare.Read);
            
            using(var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                foreach (var wallet in depositWallets)
                {
                    await writer.WriteLineAsync($"{wallet.UserId},{wallet.CryptoCurrency},{wallet.Address}");
                }
            }
        }

        public void Dispose()
        {
            _concurrencySemaphore?.Dispose();
        }

        private async Task<IReadOnlyCollection<DepositWallet>> LoadDepositWalletsAsync()
        {
            _logger.LogInformation("Loading deposit wallets...");

            var allWallets = new HashSet<DepositWallet>(131072); // 2 ^ 17

            int loadedDepositWalletsCount = 0;

            async Task<IReadOnlyCollection<DepositWallet>> LoadProviderWalletsAsync(IDepositWalletsProvider provider)
            {
                PaginatedList<DepositWallet> wallets = null;
                var allWalletsOfProvider = new List<DepositWallet>(65536);

                do
                {
                    wallets = await Policy
                        .Handle<Exception>(ex =>
                        {
                            _logger.LogWarning(ex, $"Failed to get deposits wallets using {provider.GetType().Name}. Operation will be retried.");
                            return true;
                        })
                        .WaitAndRetryForeverAsync(i => TimeSpan.FromSeconds(Math.Min(i, 5)))
                        // ReSharper disable once AccessToModifiedClosure
                        .ExecuteAsync(async () => await provider.GetWalletsAsync(wallets?.Continuation));

                    foreach (var wallet in wallets.Items.Where(x => x.Address != null))
                    {
                        allWalletsOfProvider.Add(wallet);

                        var currentLoadedDepositWalletsCount = Interlocked.Increment(ref loadedDepositWalletsCount);

                        if (currentLoadedDepositWalletsCount % 1000 == 0)
                        {
                            _logger.LogInformation($"{currentLoadedDepositWalletsCount} deposit wallets loaded so far");
                        }
                    }

                } while (wallets.Continuation != null);

                return allWalletsOfProvider;
            }

            var tasks = new List<Task<IReadOnlyCollection<DepositWallet>>>();

            foreach (var provider in _depositWalletsProviders)
            {
                tasks.Add(LoadProviderWalletsAsync(provider));
            }

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                foreach (var wallet in task.Result)
                {
                    allWallets.Add(wallet);
                }
            }

            _logger.LogInformation($"Deposit wallets loading done. {allWallets.Count} unique deposit wallets loaded");

            return allWallets;
        }

        private async Task ProcessDepositWalletAsync(DepositWallet wallet)
        {
            try
            {
                foreach (var historyProvider in _depositsHistoryProviders)
                {
                    if (!historyProvider.CanProvideHistoryFor(wallet))
                    {
                        continue;
                    }

                    PaginatedList<Transaction> transactions = null;
                    var processedWalletTransactionsCount = 0;

                    do
                    {
                        transactions = await Policy
                            .Handle<Exception>(ex =>
                            {
                                _logger.LogWarning(ex, $"Failed to get deposits history of {wallet.CryptoCurrency}:{wallet.Address}  using {historyProvider.GetType().Name}. Operation will be retried.");
                                return true;
                            })
                            .WaitAndRetryForeverAsync(i => TimeSpan.FromSeconds(Math.Min(i, 5)))
                            .ExecuteAsync(async () => await historyProvider.GetHistoryAsync(wallet, transactions?.Continuation));

                        foreach (var tx in transactions.Items)
                        {
                            _report.AddTransaction(tx);

                            Interlocked.Increment(ref _exportedDepositsCount);
                            ++processedWalletTransactionsCount;

                            if (processedWalletTransactionsCount % 100 == 0)
                            {
                                _logger.LogInformation($"{processedWalletTransactionsCount} deposits processed so far of {wallet.CryptoCurrency}:{wallet.Address} wallet using {historyProvider.GetType().Name}");
                            }
                        }
                        
                    } while (transactions.Continuation != null);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to process deposit wallet: {wallet.CryptoCurrency}:{wallet.Address}:{wallet.UserId}", ex);
            }
            finally
            {
                var processedWalletsCount = Interlocked.Increment(ref _processedWalletsCount);

                if (processedWalletsCount % 100 == 0)
                {
                    var completedPercent = processedWalletsCount * 100 / _totalDepositWalletsCount;
                    _logger.LogInformation($"{processedWalletsCount} wallets processed so far ({completedPercent}%). {_exportedDepositsCount} deposits exported so far.");
                }

                _concurrencySemaphore.Release();
            }
        }
    }
}
