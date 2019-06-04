using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Deposits;
using Lykke.Tools.ChainalysisHistoryExporter.Withdrawals;
using Microsoft.Extensions.Logging;

namespace Lykke.Tools.ChainalysisHistoryExporter.Common
{
    internal class Exporter
    {
        private readonly ILogger<Exporter> _logger;
        private readonly WithdrawalsExporter _withdrawalsExporter;
        private readonly DepositsExporter _depositsExporter;

        public Exporter(
            ILogger<Exporter> logger,
            WithdrawalsExporter withdrawalsExporter, 
            DepositsExporter depositsExporter)
        {
            _logger = logger;
            _withdrawalsExporter = withdrawalsExporter;
            _depositsExporter = depositsExporter;
        }

        public async Task ExportAsync()
        {
            _logger.LogInformation("Exporting...");

            await _withdrawalsExporter.ExportAsync();
            await _depositsExporter.ExportAsync();

            _logger.LogInformation("Exporting done");
        }
    }
}
