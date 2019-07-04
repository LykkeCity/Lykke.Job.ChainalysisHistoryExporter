using System;
using System.Threading.Tasks;
using Lykke.Job.ChainalysisHistoryExporter.Deposits;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Lykke.Job.ChainalysisHistoryExporter.Withdrawals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lykke.Job.ChainalysisHistoryExporter.Common
{
    public class Exporter
    {
        private readonly TransactionsReportBuilder _transactionsReportBuilder;
        private readonly ILogger<Exporter> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly DepositsExporter _depositsExporter;

        public Exporter(
            TransactionsReportBuilder transactionsReportBuilder,
            ILogger<Exporter> logger,
            IServiceProvider serviceProvider, 
            DepositsExporter depositsExporter)
        {
            _transactionsReportBuilder = transactionsReportBuilder;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _depositsExporter = depositsExporter;
        }

        public async Task ExportAsync()
        {
            _logger.LogInformation("Exporting...");

            await _transactionsReportBuilder.LoadSnapshotAsync();

            {
                // Localizes lifetime of the withdrawals exported to free up consumed memory when it finished.
                var withdrawalsExporter = _serviceProvider.GetRequiredService<WithdrawalsExporter>();

                await withdrawalsExporter.ExportAsync();

                // ReSharper disable once RedundantAssignment
                withdrawalsExporter = null;
            }

            await _depositsExporter.ExportAsync();

            await _transactionsReportBuilder.SaveIncrementAsync();
            await _transactionsReportBuilder.SaveSnapshotAsync();

            _logger.LogInformation("Exporting done.");
        }
    }
}
