using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Job.ChainalysisHistoryExporter.AddressNormalization;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Configuration;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Transaction = Lykke.Job.ChainalysisHistoryExporter.Reporting.Transaction;

namespace Lykke.Job.ChainalysisHistoryExporter.Withdrawals
{
    public class WithdrawalsExporter
    {
        private readonly ILogger<WithdrawalsExporter> _logger;
        private readonly TransactionsReportBuilder _transactionsReportBuilder;
        private readonly AddressNormalizer _addressNormalizer;
        private readonly IReadOnlyCollection<IWithdrawalsHistoryProvider> _withdrawalsHistoryProviders;
        private int _exportedWithdrawalsCount;

        public WithdrawalsExporter(
            ILogger<WithdrawalsExporter> logger,
            TransactionsReportBuilder transactionsReportBuilder,
            IEnumerable<IWithdrawalsHistoryProvider> withdrawalsHistoryProviders,
            IOptions<WithdrawalHistoryProvidersSettings> withdrawalsHistoryProvidersSettings,
            AddressNormalizer addressNormalizer)
        {
            _logger = logger;
            _transactionsReportBuilder = transactionsReportBuilder;
            _addressNormalizer = addressNormalizer;
            _withdrawalsHistoryProviders = withdrawalsHistoryProviders
                .Where(x => withdrawalsHistoryProvidersSettings.Value.Providers?.Contains(x.GetType().Name) ?? false)
                .ToArray();
        }

        public async Task ExportAsync()
        {
            _logger.LogInformation("Exporting withdrawals...");
            _logger.LogInformation($"Withdrawals history providers: {string.Join(", ", _withdrawalsHistoryProviders.Select(x => x.GetType().Name))}");

            var tasks = new List<Task>();

            foreach (var historyProvider in _withdrawalsHistoryProviders)
            {
                tasks.Add(ExportProviderWithdrawals(historyProvider));
            }

            await Task.WhenAll(tasks);

            _logger.LogInformation($"Withdrawals exporting done. {_exportedWithdrawalsCount} withdrawals exported");
        }

        private async Task ExportProviderWithdrawals(IWithdrawalsHistoryProvider historyProvider)
        {
            PaginatedList<Transaction> transactions = null;

            do
            {
                transactions = await Policy
                    .Handle<Exception>(ex =>
                    {
                        _logger.LogWarning(ex,
                            $"Failed to get withdrawals history using {historyProvider.GetType().Name}. Operation will be retried.");
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

                    _transactionsReportBuilder.AddTransaction(normalizedTransaction);

                    var exportedWithdrawalsCount = Interlocked.Increment(ref _exportedWithdrawalsCount);

                    if (exportedWithdrawalsCount % 1000 == 0)
                    {
                        _logger.LogInformation($"{exportedWithdrawalsCount} withdrawals exported so far");
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

            var outputAddress = _addressNormalizer.NormalizeOrDefault(tx.OutputAddress, tx.CryptoCurrency);
            if (outputAddress == null)
            {
                _logger.LogWarning($"Address {tx.OutputAddress} is not valid for {tx.CryptoCurrency}, skipping");
                return null;
            }

            return new Transaction(tx.CryptoCurrency, tx.Hash, tx.UserId, outputAddress, tx.Type);
        }
    }
}
