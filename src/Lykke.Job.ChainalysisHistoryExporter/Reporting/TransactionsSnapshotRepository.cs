using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace Lykke.Job.ChainalysisHistoryExporter.Reporting
{
    public class TransactionsSnapshotRepository
    {
        private readonly ILog _log;
        private readonly TransactionsReportReader _reader;
        private readonly TransactionsReportWriter _writer;
        private readonly CloudBlockBlob _blob;
        
        public TransactionsSnapshotRepository(
            ILogFactory logFactory,
            AzureStorageSettings settings,
            TransactionsReportReader reader,
            TransactionsReportWriter writer)
        {
            _log = logFactory.CreateLog(this);
            _reader = reader;
            _writer = writer;

            var azureAccount = CloudStorageAccount.Parse(settings.ReportStorageConnString);

            var blobClient = azureAccount.CreateCloudBlobClient();
            var blobContainer = blobClient.GetContainerReference("chainalysis-history-exporter");

            blobContainer.CreateIfNotExistsAsync().GetAwaiter().GetResult();

            _blob = blobContainer.GetBlockBlobReference("full-report.csv");
        }

        public async Task<DateTimeOffset?> GetLastModifiedAsync()
        {
            await _blob.FetchAttributesAsync();

            return _blob.Properties.LastModified;
        }

        public async Task<(HashSet<Transaction> Snapshot, DateTimeOffset? LastModified)> LoadAsync()
        {
            _log.Info("Loading transactions snapshot...");

            var snapshot = new HashSet<Transaction>(1048576);

            await _blob.FetchAttributesAsync();

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

            _log.Info($"Transactions snapshot with {snapshot.Count} transactions loaded");

            return (snapshot, _blob.Properties.LastModified);
        }

        public async Task SaveAsync(HashSet<Transaction> snapshot)
        {
            _log.Info("Saving transactions snapshot...");

            using (var stream = new MemoryStream())
            {
                await _writer.WriteAsync(snapshot, stream, leaveOpen: true);

                stream.Position = 0;

                await _blob.UploadFromStreamAsync(stream);
            }

            _log.Info($"Transactions snapshot with {snapshot.Count} transactions saved");
        }
    }
}
