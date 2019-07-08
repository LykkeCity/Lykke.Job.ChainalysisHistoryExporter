using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using SlackAPI;

namespace Lykke.Job.ChainalysisHistoryExporter.Reporting.TransactionsIncrementRepositories
{
    public class SlackTransactionsIncrementRepository : ITransactionsIncrementRepository
    {
        private readonly ILog _log;
        private readonly TransactionsReportWriter _writer;
        private readonly SlackSettings _settings;
        private readonly SlackTaskClient _client;


        public SlackTransactionsIncrementRepository(
            ILogFactory logFactory,
            TransactionsReportWriter writer,
            SlackSettings settings)
        {
            _log = logFactory.CreateLog(this);
            _writer = writer;
            _settings = settings;
            _client = new SlackTaskClient(settings.AuthToken);
        }

        public async Task SaveAsync(HashSet<Transaction> increment, DateTime incrementFrom, DateTime incrementTo)
        {
            var fileName = $"transactions-{incrementTo:s}.csv";

            _log.Info($"Uploading transactions increment to Slack {fileName}...");

            using (var stream = new MemoryStream())
            {
                await _writer.WriteAsync(increment, stream, leaveOpen: true);

                stream.Position = 0;

                await _client.UploadFileAsync
                (
                    stream.ToArray(),
                    fileName,
                    new[] {_settings.ReportChannel},
                    title: $"Transactions for the period {incrementFrom:s} - {incrementTo:s}"
                );
            }

            _log.Info($"Transactions increment with {increment.Count} transactions uploaded to Slack");
        }
    }
}
