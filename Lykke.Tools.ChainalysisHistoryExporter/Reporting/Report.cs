using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Microsoft.Extensions.Options;

namespace Lykke.Tools.ChainalysisHistoryExporter.Reporting
{
    internal class Report : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly SemaphoreSlim _lock;

        public Report(IOptions<ReportSettings> settings)
        {
            var stream = File.Open(settings.Value.FilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(stream, Encoding.UTF8);

            _writer.WriteLineAsync("user-id,cryptocurrency,tx-type,tx-hash,output-address");

            _lock = new SemaphoreSlim(1);
        }

        public async Task AddTransactionAsync(Transaction tx)
        {
            await _lock.WaitAsync();

            try
            {
                await _writer.WriteLineAsync($"{tx.UserId},{tx.CryptoCurrency},{GetTransactionType(tx)},{tx.Hash},{tx.OutputAddress}");
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        private static string GetTransactionType(Transaction tx)
        {
            return tx.Type == TransactionType.Deposit ? "received" : "sent";
        }
    }
}
