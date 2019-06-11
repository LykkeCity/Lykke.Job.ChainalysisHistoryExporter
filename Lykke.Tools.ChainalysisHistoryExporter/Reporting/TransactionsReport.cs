using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lykke.Tools.ChainalysisHistoryExporter.Reporting
{
    public class TransactionsReport
    {
        private readonly ILogger<TransactionsReport> _logger;
        private readonly IOptions<ReportSettings> _settings;
        private readonly HashSet<Transaction> _transactions;
        private bool _saved;

        public TransactionsReport(
            ILogger<TransactionsReport> logger,
            IOptions<ReportSettings> settings)
        {
            _logger = logger;
            _settings = settings;
            _transactions = new HashSet<Transaction>(1048576);
        }

        public void AddTransaction(Transaction tx)
        {
            if (_saved)
            {
                throw new InvalidOperationException("Report already saved");
            }

            if (string.IsNullOrWhiteSpace(tx.Hash) ||
                string.IsNullOrWhiteSpace(tx.CryptoCurrency) ||
                tx.UserId == Guid.Empty)
            {
                return;
            }

            _transactions.Add(tx);
        }

        public async Task SaveAsync()
        {
            _saved = true;

            var filePath = _settings.Value.TransactionsFilePath;

            _logger.LogInformation($"Saving transactions report to {filePath}...");

            var stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                await writer.WriteLineAsync("user-id,cryptocurrency,transaction-type,transaction-hash,output-address");

                foreach (var tx in _transactions)
                {
                    await writer.WriteLineAsync($"{tx.UserId},{tx.CryptoCurrency},{GetTransactionType(tx)},{tx.Hash},{tx.OutputAddress}");    
                }
            }

            _logger.LogInformation($"Transactions report saving done. {_transactions.Count} unique transactions saved");
        }

        private static string GetTransactionType(Transaction tx)
        {
            return tx.Type == TransactionType.Deposit ? "received" : "sent";
        }
    }
}
