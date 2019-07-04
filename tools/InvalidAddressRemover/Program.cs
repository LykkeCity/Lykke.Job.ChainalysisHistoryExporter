using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.AddressNormalization;
using Lykke.Job.ChainalysisHistoryExporter.Assets;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Configuration;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Lykke.Logs;
using Lykke.Logs.Loggers.LykkeConsole;
using Microsoft.Extensions.Options;
using NBitcoin.Altcoins;
using Transaction = Lykke.Job.ChainalysisHistoryExporter.Reporting.Transaction;

namespace InvalidAddressRemover
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Litecoin.Instance.EnsureRegistered();
            BCash.Instance.EnsureRegistered();

            using (var logFactory = LogFactory.Create().AddConsole())
            {
                await RemoveInvalidAddresses(logFactory);
            }
        }

        private static async Task RemoveInvalidAddresses(ILogFactory logFactory)
        {
            var assetsClient = new AssetsClient(logFactory, Options.Create(new ServicesSettings()));
            var blockchainProvider = new BlockchainsProvider(assetsClient);
            var addressNormalizer = new AddressNormalizer
            (
                logFactory,
                new IAddressNormalizer[]
                {
                    new GeneralAddressNormalizer(),
                    new BtcAddressNormalizer(blockchainProvider, Options.Create(new BtcSettings {Network = "mainnet"})),
                    new BchAddressNormalizer(blockchainProvider, Options.Create(new BchSettings {Network = "mainnet"})),
                    new LtcAddressNormalizer(blockchainProvider, Options.Create(new LtcSettings {Network = "ltc-main"})),
                    new EthAddressNormalizer(blockchainProvider),
                }
            );
            var reportReader = new TransactionsReportReader(addressNormalizer);
            var reportWriter = new TransactionsReportWriter();

            Console.WriteLine("Loading...");

            var readStream = File.Open("transactions.csv", FileMode.Open, FileAccess.Read, FileShare.Read);
            var originalTransactions = await reportReader.ReadAsync
            (
                readStream,
                new Progress<int>
                (
                    transactionCount =>
                    {
                        if (transactionCount % 1000 == 0)
                        {
                            Console.WriteLine(transactionCount);
                        }
                    }
                ),
                leaveOpen: false
            );

            Console.WriteLine("Filtering...");

            var filteredTransactions = originalTransactions
                .Where
                (
                    tx => !string.IsNullOrWhiteSpace(tx.Hash) &&
                          !string.IsNullOrWhiteSpace(tx.CryptoCurrency) &&
                          tx.UserId != Guid.Empty
                )
                .ToHashSet();

            Console.WriteLine("Saving...");

            var writeStream = File.Open
            (
                "filtered-transactions.csv",
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read
            );
            await reportWriter.WriteAsync
            (
                filteredTransactions,
                writeStream,
                new Progress<int>
                (
                    transactionsCount =>
                    {
                        if (transactionsCount % 1000 == 0)
                        {
                            var percent = transactionsCount * 100 / filteredTransactions.Count;

                            Console.WriteLine($"{percent}%");
                        }
                    }
                ),
                leaveOpen: false
            );
        }
    }
}
