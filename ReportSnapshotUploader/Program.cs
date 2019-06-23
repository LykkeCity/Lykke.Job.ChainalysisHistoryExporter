using System;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;

namespace ReportSnapshotUploader
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var configurationBuilder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddJsonFile("appsettings.json", optional: true)
                .AddCommandLine(args, Settings.CommandLineMappings);
            var configuration = configurationBuilder.Build();
            var settings = configuration.Get<Settings>();

            if (!settings.Validate())
            {
                return;
            }

            await UploadReportSnapshot(settings.AzureConnectionString, settings.FilePath);
        }

        private static async Task UploadReportSnapshot(string connectionString, string filePath)
        {
            Console.WriteLine("Parsing connection string...");

            var azureAccount = CloudStorageAccount.Parse(connectionString);

            Console.WriteLine($"Initializing BLOB container in Azure Storage account {azureAccount.Credentials.AccountName}...");

            var blobClient = azureAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("chainalysis-history-exporter");

            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference("full-report.csv");

            Console.WriteLine($"Start uploading file: {filePath}...");

            await blob.UploadFromFileAsync(filePath);

            Console.WriteLine("Done");
        }
    }
}
