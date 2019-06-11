using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace Lykke.Tools.ChainalysisHistoryExporter.Deposits
{
    public class DepositsExporter : IDisposable
    {
        private readonly ILogger<DepositsExporter> _logger;
        private readonly TransactionsReport _transactionsReport;
        private readonly DepositWalletsReport _depositWalletsReport;
        private readonly IReadOnlyCollection<IDepositWalletsProvider> _depositWalletsProviders;
        private readonly IReadOnlyCollection<IDepositsHistoryProvider> _depositsHistoryProviders;
        private readonly SemaphoreSlim _concurrencySemaphore;
        private int _processedWalletsCount;
        private int _exportedDepositsCount;
        private int _totalDepositWalletsCount;

        public DepositsExporter(
            ILogger<DepositsExporter> logger,
            TransactionsReport transactionsReport,
            DepositWalletsReport depositWalletsReport,
            IEnumerable<IDepositWalletsProvider> depositWalletsProviders,
            IEnumerable<IDepositsHistoryProvider> depositsHistoryProviders,
            IOptions<DepositWalletProvidersSettings> depositWalletsProvidersSettings,
            IOptions<DepositHistoryProvidersSettings> depositHistoryProvidersSettings)
        {
            _logger = logger;
            _transactionsReport = transactionsReport;
            _depositWalletsReport = depositWalletsReport;
            _depositWalletsProviders = depositWalletsProviders
                .Where(x => depositWalletsProvidersSettings.Value.Providers?.Contains(x.GetType().Name) ?? false)
                .ToArray();
            _depositsHistoryProviders = depositsHistoryProviders
                .Where(x => depositHistoryProvidersSettings.Value.Providers?.Contains(x.GetType().Name) ?? false)
                .ToArray();

            _concurrencySemaphore = new SemaphoreSlim(8);
        }

        public async Task ExportAsync()
        {
            var depositWallets = await LoadDepositWalletsAsync();

            _totalDepositWalletsCount = depositWallets.Count;

            await _depositWalletsReport.SaveAsync(depositWallets);

            var tasks = new List<Task>(512);

            _logger.LogInformation("Exporting deposits...");
            _logger.LogInformation($"Deposits history providers: {string.Join(", ", _depositsHistoryProviders.Select(x => x.GetType().Name))}");

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

            await Task.WhenAll(tasks);

            _logger.LogInformation($"Deposits exporting done. {_processedWalletsCount} deposit wallets processed. {_exportedDepositsCount} deposits exported");
        }

        public void Dispose()
        {
            _concurrencySemaphore?.Dispose();
        }

        private async Task<ISet<DepositWallet>> LoadDepositWalletsAsync()
        {
            _logger.LogInformation("Loading deposit wallets...");
            _logger.LogInformation($"Deposit wallets providers: {string.Join(", ", _depositWalletsProviders.Select(x => x.GetType().Name))}");

            var allWallets = new HashSet<DepositWallet>(131072); // 2 ^ 17

            var loadedDepositWalletsCount = 0;

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

                    foreach (var wallet in wallets.Items.Where(x => !string.IsNullOrWhiteSpace(x.Address)))
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
                            _transactionsReport.AddTransaction(tx);

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
