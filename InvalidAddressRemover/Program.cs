using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.AddressNormalization;
using Lykke.Tools.ChainalysisHistoryExporter.Assets;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NBitcoin.Altcoins;
using Transaction = Lykke.Tools.ChainalysisHistoryExporter.Reporting.Transaction;

namespace InvalidAddressRemover
{
    public class Logger<T> : ILogger<T>, IDisposable
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new Logger<T>();
        }

        public void Dispose()
        {
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            Litecoin.Instance.EnsureRegistered();
            BCash.Instance.EnsureRegistered();
            
            var assetsClient = new AssetsClient(new Logger<AssetsClient>(), Options.Create(new ServicesSettings()));
            var blockchainProvider = new BlockchainsProvider(assetsClient);
            var addressNormalizer = new AddressNormalizer
            (
                new IAddressNormalizer[]
                {
                    new GeneralAddressNormalizer(),
                    new BtcAddressNormalizer(blockchainProvider, Options.Create(new BtcSettings {Network = "mainnet"})),
                    new BchAddressNormalizer(blockchainProvider, Options.Create(new BchSettings {Network = "mainnet"})),
                    new LtcAddressNormalizer(blockchainProvider, Options.Create(new LtcSettings {Network = "ltc-main"})),
                    new EthAddressNormalizer(blockchainProvider),
                }
            );
            var report = new TransactionsReport
            (
                new Logger<TransactionsReport>(),
                Options.Create(new ReportSettings {TransactionsFilePath = "filtered-transactions.csv"})
            );

            int readLinesCount = 0;

            var stream = File.Open("transactions.csv", FileMode.Open, FileAccess.Read, FileShare.Read);
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                await reader.ReadLineAsync();

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    var parts = line.Split(",");

                    if (parts.Length != 5)
                    {
                        continue;
                    }

                    if (!Guid.TryParse(parts[0], out var userId))
                    {
                        Console.WriteLine($"User ID {parts[0]} is invalid GUID. Skipping");
                        continue;
                    }

                    var cryptoCurrency = parts[1];
                    var transactionType = parts[2] == "sent" ? TransactionType.Withdrawal : TransactionType.Deposit;
                    var transactionHash = parts[3];
                    var outputAddress = addressNormalizer.NormalizeOrDefault(parts[4], cryptoCurrency);

                    if (outputAddress == null)
                    {
                        Console.WriteLine($"Address {parts[4]} is invalid for {cryptoCurrency}. Skipping");
                        continue;
                    }

                    var transaction = new Transaction(cryptoCurrency, transactionHash, userId, outputAddress, transactionType);

                    report.AddTransaction(transaction);

                    ++readLinesCount;

                    if (readLinesCount % 1000 == 0)
                    {
                        Console.WriteLine(readLinesCount);
                    }
                }
            }

            await report.SaveAsync();
        }
    }
}
