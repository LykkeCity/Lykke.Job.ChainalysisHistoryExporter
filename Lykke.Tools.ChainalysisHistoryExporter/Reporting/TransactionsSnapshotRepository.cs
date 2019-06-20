using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lykke.Tools.ChainalysisHistoryExporter.Reporting
{
    public class TransactionsSnapshotRepository
    {
        private readonly ILogger<TransactionsSnapshotRepository> _logger;
        private readonly TransactionsReportReader _reader;
        private readonly TransactionsReportWriter _writer;
        private readonly CloudBlockBlob _blob;


        public TransactionsSnapshotRepository(
            ILogger<TransactionsSnapshotRepository> logger,
            IOptions<ReportSettings> settings,
            TransactionsReportReader reader,
            TransactionsReportWriter writer)
        {
            _logger = logger;
            _reader = reader;
            _writer = writer;

            var azureAccount = CloudStorageAccount.Parse(settings.Value.AzureStorageConnString);

            var blobClient = azureAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("chainalysis-history-exporter");

            blobContainer.CreateIfNotExistsAsync().GetAwaiter().GetResult();

            _blob = blobContainer.GetBlockBlobReference("full-report.csv");
        }

        public async Task<HashSet<Transaction>> LoadAsync()
        {
            _logger.LogInformation("Loading transactions snapshot...");

            var snapshot = new HashSet<Transaction>(1048576);

            if (await _blob.ExistsAsync())
            {
                using (var stream = new MemoryStream())
                {
                    await _blob.DownloadToStreamAsync(stream);

                    stream.Position = 0;

                    var transactions = await _reader.ReadAsync(stream, leaveOpen: true);

                    foreach (var transaction in transactions)
                    {
                        snapshot.Add(transaction);
                    }
                }
            }

            _logger.LogInformation($"Transactions snapshot with {snapshot.Count} transactions loaded");

            return snapshot;
        }

        public async Task SaveAsync(HashSet<Transaction> snapshot)
        {
            _logger.LogInformation($"Saving transactions snapshot...");

            using (var stream = new MemoryStream())
            {
                await _writer.WriteAsync(snapshot, stream, leaveOpen: true);

                stream.Position = 0;

                await _blob.UploadFromStreamAsync(stream);
            }

            _logger.LogInformation($"Transactions snapshot with {snapshot.Count} transactions saved");
        }
    }
}
