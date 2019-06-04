using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;
using Microsoft.Extensions.Logging;
using Polly;

namespace Lykke.Tools.ChainalysisHistoryExporter.Withdrawals
{
    internal class WithdrawalsExporter
    {
        private readonly ILogger<WithdrawalsExporter> _logger;
        private readonly Report _report;
        private readonly IEnumerable<IWithdrawalsHistoryProvider> _withdrawalsHistoryProviders;

        public WithdrawalsExporter(
            ILogger<WithdrawalsExporter> logger,
            Report report,
            IEnumerable<IWithdrawalsHistoryProvider> withdrawalsHistoryProviders)
        {
            _logger = logger;
            _report = report;
            _withdrawalsHistoryProviders = withdrawalsHistoryProviders;
        }

        public async Task ExportAsync()
        {
            _logger.LogInformation("Exporting withdrawals...");

            foreach (var historyProvider in _withdrawalsHistoryProviders)
            {
                PaginatedList<Transaction> transactions = null;
                var batchNumber = 1;

                do
                {
                    _logger.LogInformation($"Exporting withdrawals batch {batchNumber} using {historyProvider.GetType().Name}");

                    transactions = await Policy
                        .Handle<Exception>(ex =>
                        {
                            _logger.LogWarning(ex, $"Failed to get withdrawals history using {historyProvider.GetType().Name}. Operation will be retried.");
                            return true;
                        })
                        .WaitAndRetryForeverAsync(i => TimeSpan.FromSeconds(Math.Min(i, 5)))
                        .ExecuteAsync(async () => await historyProvider.GetHistoryAsync(transactions?.Continuation));

                    foreach (var tx in transactions.Items)
                    {
                        await _report.AddTransactionAsync(tx);
                    }

                    batchNumber++;

                } while (transactions.Continuation != null);
            }

            _logger.LogInformation("Withdrawals exporting done");
        }
    }
}
