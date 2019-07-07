using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Settings;

namespace Lykke.Job.ChainalysisHistoryExporter.Reporting
{
    public class TransactionsIncrementRepository
    {
        private readonly ILog _log;
        private readonly ReportSettings _settings;
        private readonly TransactionsReportWriter _writer;
        
        public TransactionsIncrementRepository(
            ILogFactory logFactory,
            ReportSettings settings,
            TransactionsReportWriter writer)
        {
            _log = logFactory.CreateLog(this);
            _settings = settings;
            _writer = writer;
        }

        public async Task SaveAsync(HashSet<Transaction> increment)
        {
            var filePath = _settings.TransactionsFilePath
                .Replace("{date}", DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm"));

            _log.Info($"Saving transactions increment to {filePath}...");

            var stream = File.Open
            (
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read
            );
            await _writer.WriteAsync(increment, stream, leaveOpen: false);

            _log.Info($"Transactions increment with {increment.Count} transactions saved");
        }
    }
}
