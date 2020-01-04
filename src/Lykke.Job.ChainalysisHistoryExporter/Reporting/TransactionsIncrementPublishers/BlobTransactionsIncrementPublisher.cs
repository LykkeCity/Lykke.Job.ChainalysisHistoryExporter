using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace Lykke.Job.ChainalysisHistoryExporter.Reporting.TransactionsIncrementPublishers
{
    public class BlobTransactionsIncrementPublisher : ITransactionsIncrementPublisher
    {
        private readonly ILog _log;
        private readonly TransactionsReportWriter _writer;
        private readonly CloudBlobContainer _blobContainer;

        public BlobTransactionsIncrementPublisher(
            ILogFactory logFactory,
            AzureStorageSettings settings,
            TransactionsReportWriter writer)
        {
            _log = logFactory.CreateLog(this);

            _writer = writer;
            
            var azureAccount = CloudStorageAccount.Parse(settings.ReportStorageConnString);

            var blobClient = azureAccount.CreateCloudBlobClient();
            _blobContainer = blobClient.GetContainerReference("chainalysis-history-exporter");

            _blobContainer.CreateIfNotExistsAsync().GetAwaiter().GetResult();
        }

        public async Task Publish(HashSet<Transaction> increment, DateTime incrementFrom, DateTime incrementTo)
        {
            var blobName = $"increment-from-{incrementFrom:s}.csv";

            _log.Info($"Saving transactions increment to the BLOB {blobName}...");

            var blob = _blobContainer.GetBlockBlobReference(blobName);

            using (var stream = new MemoryStream())
            {
                await _writer.WriteAsync(increment, stream, leaveOpen: true);

                stream.Position = 0;
                
                await blob.UploadFromStreamAsync(stream);
            }

            _log.Info($"Transactions increment with {increment.Count} transactions saved to the BLOB");
        }
    }
}
