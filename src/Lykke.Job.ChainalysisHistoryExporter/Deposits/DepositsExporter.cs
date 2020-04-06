using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.AddressNormalization;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Polly;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits
{
    public class DepositsExporter : IDisposable
    {
        private readonly ILog _log;
        private readonly AddressNormalizer _addressNormalizer;
        private readonly IReadOnlyCollection<IDepositWalletsProvider> _depositWalletsProviders;
        private readonly IReadOnlyCollection<IDepositsHistoryProvider> _depositsHistoryProviders;
        private readonly SemaphoreSlim _concurrencySemaphore;
        private int _processedWalletsCount;
        private int _exportedDepositsCount;
        private int _totalDepositWalletsCount;
        
        public DepositsExporter(
            ILogFactory logFactory,
            AddressNormalizer addressNormalizer,
            IReadOnlyCollection<IDepositWalletsProvider> depositWalletsProviders,
            IReadOnlyCollection<IDepositsHistoryProvider> depositsHistoryProviders)
        {
            _log = logFactory.CreateLog(this);
            _addressNormalizer = addressNormalizer;
            _depositWalletsProviders = depositWalletsProviders;
            _depositsHistoryProviders = depositsHistoryProviders;

            _concurrencySemaphore = new SemaphoreSlim(8);
        }

        public async Task ExportAsync(TransactionsReportBuilder reportBuilder)
        {
            var depositWallets = await LoadDepositWalletsAsync();

            _totalDepositWalletsCount = depositWallets.Count;

            var tasks = new List<Task>(512);

            _log.Info("Exporting deposits...", new
            {
                DepositsHistoryProvider = _depositsHistoryProviders.Select(x => x.GetType().Name)
            });

            foreach (var wallet in depositWallets)
            {
                await _concurrencySemaphore.WaitAsync();

                tasks.Add(ProcessDepositWalletAsync(reportBuilder, wallet));

                if (tasks.Count >= 500)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }

            await Task.WhenAll(tasks);

            _log.Info($"Deposits exporting done. {_processedWalletsCount} deposit wallets processed. {_exportedDepositsCount} deposits exported");
        }

        public void Dispose()
        {
            _concurrencySemaphore?.Dispose();
        }

        private async Task<ISet<DepositWallet>> LoadDepositWalletsAsync()
        {
            _log.Info("Loading deposit wallets...", new
            {
                DepositWalletsProviders = _depositWalletsProviders.Select(x => x.GetType().Name)
            });

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
                            _log.Warning($"Failed to get deposits wallets using {provider.GetType().Name}. Operation will be retried.",
                                ex);
                            return true;
                        })
                        .WaitAndRetryForeverAsync(i => TimeSpan.FromSeconds(Math.Min(i, 5)))
                        // ReSharper disable once AccessToModifiedClosure
                        .ExecuteAsync(async () => await provider.GetWalletsAsync(wallets?.Continuation));

                    foreach (var wallet in wallets.Items)
                    {
                        var normalizedWallet = NormalizeWalletOrDefault(wallet);
                        if (normalizedWallet == null)
                        {
                            continue;
                        }

                        allWalletsOfProvider.Add(normalizedWallet);

                        var currentLoadedDepositWalletsCount = Interlocked.Increment(ref loadedDepositWalletsCount);

                        if (currentLoadedDepositWalletsCount % 1000 == 0)
                        {
                            _log.Info($"{currentLoadedDepositWalletsCount} deposit wallets loaded so far");
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

            _log.Info($"Deposit wallets loading done. {allWallets.Count} unique deposit wallets loaded");

            return allWallets;
        }

        private DepositWallet NormalizeWalletOrDefault(DepositWallet wallet)
        {
            if (string.IsNullOrWhiteSpace(wallet.Address) ||
                string.IsNullOrWhiteSpace(wallet.CryptoCurrency) ||
                wallet.UserId == Guid.Empty)
            {
                return null;
            }

            var address = _addressNormalizer.NormalizeOrDefault(wallet.Address, wallet.CryptoCurrency);
            if (address == null)
            {
                _log.Warning
                (
                    "It is not a valid address, skipping",
                    context: new
                    {
                        Address = wallet.Address, 
                        CryptoCurrency = wallet.CryptoCurrency
                    }
                );
                return null;
            }

            return new DepositWallet(wallet.UserId, address, wallet.CryptoCurrency);
        }

        private async Task ProcessDepositWalletAsync(TransactionsReportBuilder reportBuilder, DepositWallet wallet)
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
                                _log.Warning
                                (
                                    "Failed to get deposits history. Operation will be retried.",
                                    context: new
                                    {
                                        Address = wallet.Address,
                                        CryptoCurrency = wallet.CryptoCurrency,
                                        HistoryProvider = historyProvider.GetType().Name
                                    },
                                    exception: ex
                                );
                                return true;
                            })
                            .WaitAndRetryForeverAsync(i => TimeSpan.FromSeconds(Math.Min(i, 5)))
                            .ExecuteAsync(async () => await historyProvider.GetHistoryAsync(wallet, transactions?.Continuation));

                        foreach (var tx in transactions.Items)
                        {
                            var normalizedTransaction = NormalizeTransactionOrDefault(tx);
                            if (normalizedTransaction == null)
                            {
                                continue;
                            }

                            reportBuilder.AddTransaction(normalizedTransaction);

                            Interlocked.Increment(ref _exportedDepositsCount);
                            ++processedWalletTransactionsCount;

                            if (processedWalletTransactionsCount % 100 == 0)
                            {
                                _log.Info($"{processedWalletTransactionsCount} deposits processed so far of {wallet.CryptoCurrency}:{wallet.Address} wallet using {historyProvider.GetType().Name}");
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
                    _log.Info($"{processedWalletsCount} wallets processed so far ({completedPercent}%). {_exportedDepositsCount} deposits exported so far.");
                }

                _concurrencySemaphore.Release();
            }
        }

        private Transaction NormalizeTransactionOrDefault(Transaction tx)
        {
            if (string.IsNullOrWhiteSpace(tx.Hash) ||
                string.IsNullOrWhiteSpace(tx.CryptoCurrency) ||
                tx.UserId == Guid.Empty)
            {
                return null;
            }

            var outputAddress = _addressNormalizer.NormalizeOrDefault(tx.OutputAddress, tx.CryptoCurrency, isTransactionNormalization: true);
            if (outputAddress == null)
            {
                _log.Warning
                (
                    "It is not a valid address, skipping",
                    context: new
                    {
                        Address = tx.OutputAddress, 
                        CryptoCurrency = tx.CryptoCurrency
                    }
                );
                return null;
            }

            return new Transaction(tx.CryptoCurrency, tx.Hash, tx.UserId, outputAddress, tx.Type);
        }
    }
}
