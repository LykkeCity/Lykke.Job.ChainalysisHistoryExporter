using System;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Deposits;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;
using Lykke.Tools.ChainalysisHistoryExporter.Withdrawals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lykke.Tools.ChainalysisHistoryExporter.Common
{
    public class Exporter
    {
        private readonly TransactionsReport _transactionsReport;
        private readonly ILogger<Exporter> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly DepositsExporter _depositsExporter;

        public Exporter(
            TransactionsReport transactionsReport,
            ILogger<Exporter> logger,
            IServiceProvider serviceProvider, 
            DepositsExporter depositsExporter)
        {
            _transactionsReport = transactionsReport;
            _logger = logger;
            _serviceProvider = serviceProvider;
            _depositsExporter = depositsExporter;
        }

        public async Task ExportAsync()
        {
            _logger.LogInformation("Exporting...");
            
            {
                // Localizes lifetime of the withdrawals exported to free up consumed memory when it finished.
                var withdrawalsExporter = _serviceProvider.GetRequiredService<WithdrawalsExporter>();

                await withdrawalsExporter.ExportAsync();

                // ReSharper disable once RedundantAssignment
                withdrawalsExporter = null;
            }

            await _depositsExporter.ExportAsync();

            await _transactionsReport.SaveAsync();

            _logger.LogInformation($"Exporting done.");
        }
    }
}
