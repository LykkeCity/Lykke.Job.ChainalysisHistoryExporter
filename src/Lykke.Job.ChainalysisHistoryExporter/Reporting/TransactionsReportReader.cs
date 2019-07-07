using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.AddressNormalization;

namespace Lykke.Tools.ChainalysisHistoryExporter.Reporting
{
    public class TransactionsReportReader
    {
        private readonly AddressNormalizer _addressNormalizer;

        public TransactionsReportReader(AddressNormalizer addressNormalizer)
        {
            _addressNormalizer = addressNormalizer;
        }

        public async Task<IReadOnlyCollection<Transaction>> ReadAsync(Stream stream, IProgress<int> progress = null, bool leaveOpen = false)
        {
            var transactions = new List<Transaction>(1048576);

            using (var reader = new StreamReader
            (
                stream,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024 * 32,
                leaveOpen: leaveOpen
            ))
            {
                await reader.ReadLineAsync();

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    var parts = line.Split(",");

                    if (parts.Length != 5)
                    {
                        throw new InvalidOperationException($"Invalid record found {line}. 5 Columns are expected, actual count is {parts.Length} ");
                    }

                    if (!Guid.TryParse(parts[0], out var userId))
                    {
                        throw new InvalidOperationException($"User ID {parts[0]} is invalid GUID");
                    }

                    var cryptoCurrency = parts[1];
                    var transactionType = parts[2] == "sent" ? TransactionType.Withdrawal : TransactionType.Deposit;
                    var transactionHash = parts[3];
                    var outputAddress = _addressNormalizer.NormalizeOrDefault(parts[4], cryptoCurrency);

                    if (outputAddress == null)
                    {
                        throw new InvalidOperationException($"Address {parts[4]} is invalid for {cryptoCurrency}");
                    }

                    var transaction = new Transaction
                    (
                        cryptoCurrency,
                        transactionHash,
                        userId,
                        outputAddress,
                        transactionType
                    );

                    transactions.Add(transaction);

                    progress?.Report(transactions.Count);
                }
            }

            return transactions;
        }
    }
}
