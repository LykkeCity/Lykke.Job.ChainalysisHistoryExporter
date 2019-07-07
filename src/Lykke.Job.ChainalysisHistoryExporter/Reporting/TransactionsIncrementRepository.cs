using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lykke.Tools.ChainalysisHistoryExporter.Reporting
{
    public class TransactionsIncrementRepository
    {
        private readonly ILogger<TransactionsIncrementRepository> _logger;
        private readonly IOptions<ReportSettings> _settings;
        private readonly TransactionsReportWriter _writer;

        public TransactionsIncrementRepository(
            ILogger<TransactionsIncrementRepository> logger,
            IOptions<ReportSettings> settings,
            TransactionsReportWriter writer)
        {
            _logger = logger;
            _settings = settings;
            _writer = writer;
        }

        public async Task SaveAsync(HashSet<Transaction> increment)
        {
            var filePath = _settings.Value.TransactionsFilePath
                .Replace("{date}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm"));

            _logger.LogInformation($"Saving transactions increment to {filePath}...");

            var stream = File.Open
            (
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read
            );
            await _writer.WriteAsync(increment, stream, leaveOpen: false);

            _logger.LogInformation($"Transactions increment with {increment.Count} transactions saved");
        }
    }
}
