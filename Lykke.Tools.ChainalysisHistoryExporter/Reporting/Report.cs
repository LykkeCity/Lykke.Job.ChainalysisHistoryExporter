using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lykke.Tools.ChainalysisHistoryExporter.Reporting
{
    public class Report
    {
        private readonly ILogger<Report> _logger;
        private readonly IOptions<ReportSettings> _settings;
        private readonly HashSet<Transaction> _transactions;

        public Report(
            ILogger<Report> logger,
            IOptions<ReportSettings> settings)
        {
            _logger = logger;
            _settings = settings;
            _transactions = new HashSet<Transaction>(1048576);
        }

        public void AddTransaction(Transaction tx)
        {
            _transactions.Add(tx);
        }

        public async Task SaveAsync()
        {
            var filePath = _settings.Value.FilePath;

            _logger.LogInformation($"Saving report to {filePath}...");

            var stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                await writer.WriteLineAsync("user-id,cryptocurrency,tx-type,tx-hash,output-address");

                foreach (var tx in _transactions)
                {
                    await writer.WriteLineAsync($"{tx.UserId},{tx.CryptoCurrency},{GetTransactionType(tx)},{tx.Hash},{tx.OutputAddress}");    
                }
            }

            _logger.LogInformation($"Report saving done. {_transactions.Count} unique transactions saved");
        }

        private static string GetTransactionType(Transaction tx)
        {
            return tx.Type == TransactionType.Deposit ? "received" : "sent";
        }
    }
}
