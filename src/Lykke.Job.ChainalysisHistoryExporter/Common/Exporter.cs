using System;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Deposits;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Lykke.Job.ChainalysisHistoryExporter.Withdrawals;
using Microsoft.Extensions.DependencyInjection;

namespace Lykke.Job.ChainalysisHistoryExporter.Common
{
    public class Exporter
    {
        private readonly ILog _log;
        private readonly TransactionsReportBuilder _transactionsReportBuilder;
        private readonly IServiceProvider _serviceProvider;
        private readonly DepositsExporter _depositsExporter;
        

        public Exporter(
            ILogFactory logFactory,
            TransactionsReportBuilder transactionsReportBuilder,
            IServiceProvider serviceProvider, 
            DepositsExporter depositsExporter)
        {
            _log = logFactory.CreateLog(this);
            _transactionsReportBuilder = transactionsReportBuilder;
            _serviceProvider = serviceProvider;
            _depositsExporter = depositsExporter;
        }

        public async Task ExportAsync()
        {
            _log.Info("Exporting...");

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

            _log.Info("Exporting done.");
        }
    }
}
