using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lykke.Tools.ChainalysisHistoryExporter.Reporting
{
    public class TransactionsReportWriter
    {
        public async Task WriteAsync(IEnumerable<Transaction> transactions, Stream stream, IProgress<int> progress = null, bool leaveOpen = false)
        {
            using (var writer = new StreamWriter
            (
                stream,
                Encoding.UTF8,
                bufferSize: 1024 * 32,
                leaveOpen: leaveOpen
            ))
            {
                await writer.WriteLineAsync("user-id,cryptocurrency,transaction-type,transaction-hash,output-address");

                var savedTransactionsCount = 0;

                foreach (var tx in transactions)
                {
                    await writer.WriteLineAsync($"{tx.UserId},{tx.CryptoCurrency},{GetTransactionType(tx)},{tx.Hash},{tx.OutputAddress}");

                    ++savedTransactionsCount;

                    progress?.Report(savedTransactionsCount);
                }
            }
        }

        private static string GetTransactionType(Transaction tx)
        {
            return tx.Type == TransactionType.Deposit ? "received" : "sent";
        }
    }
}
