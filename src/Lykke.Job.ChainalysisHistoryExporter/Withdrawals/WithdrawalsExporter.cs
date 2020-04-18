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
using Transaction = Lykke.Job.ChainalysisHistoryExporter.Reporting.Transaction;

namespace Lykke.Job.ChainalysisHistoryExporter.Withdrawals
{
    public class WithdrawalsExporter
    {
        private readonly ILog _log;
        private readonly AddressNormalizer _addressNormalizer;
        private readonly IReadOnlyCollection<IWithdrawalsHistoryProvider> _withdrawalsHistoryProviders;
        private int _exportedWithdrawalsCount;
        

        public WithdrawalsExporter(
            ILogFactory logFactory,
            IReadOnlyCollection<IWithdrawalsHistoryProvider> withdrawalsHistoryProviders,
            AddressNormalizer addressNormalizer)
        {
            _log = logFactory.CreateLog(this);
            _addressNormalizer = addressNormalizer;
            _withdrawalsHistoryProviders = withdrawalsHistoryProviders;
        }

        public async Task ExportAsync(TransactionsReportBuilder reportBuilder)
        {
            _log.Info("Exporting withdrawals...", new
            {
                WithdrawalHistoryProviders = _withdrawalsHistoryProviders.Select(x => x.GetType().Name)
            });

            var tasks = new List<Task>();

            foreach (var historyProvider in _withdrawalsHistoryProviders)
            {
                tasks.Add(ExportProviderWithdrawals(reportBuilder, historyProvider));
            }

            await Task.WhenAll(tasks);

            _log.Info($"Withdrawals exporting done. {_exportedWithdrawalsCount} withdrawals exported");
        }

        private async Task ExportProviderWithdrawals(TransactionsReportBuilder reportBuilder, IWithdrawalsHistoryProvider historyProvider)
        {
            PaginatedList<Transaction> transactions = null;

            do
            {
                transactions = await Policy
                    .Handle<Exception>(ex =>
                    {
                        _log.Warning
                        (
                            "Failed to get withdrawals history. Operation will be retried.",
                            context: new
                            {
                                WithdrawalHistoryProvider = historyProvider.GetType().Name
                            },
                            exception: ex
                        );
                        return true;
                    })
                    .WaitAndRetryForeverAsync(i => TimeSpan.FromSeconds(Math.Min(i, 5)))
                    .ExecuteAsync(async () => await historyProvider.GetHistoryAsync(transactions?.Continuation));

                foreach (var tx in transactions.Items)
                {
                    var normalizedTransaction = NormalizeTransactionOrDefault(tx);
                    if (normalizedTransaction == null)
                    {
                        continue;
                    }

                    reportBuilder.AddTransaction(normalizedTransaction);

                    var exportedWithdrawalsCount = Interlocked.Increment(ref _exportedWithdrawalsCount);

                    if (exportedWithdrawalsCount % 1000 == 0)
                    {
                        _log.Info($"{exportedWithdrawalsCount} withdrawals exported so far");
                    }
                }
            } while (transactions.Continuation != null);
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
                _log.Warning("It is not a valid address, skipping", context: new
                {
                    Address = tx.OutputAddress,
                    CryptoCurrencty = tx.CryptoCurrency
                });
                return null;
            }

            return new Transaction(tx.CryptoCurrency, tx.Hash, tx.UserId, outputAddress, tx.Type);
        }
    }
}
