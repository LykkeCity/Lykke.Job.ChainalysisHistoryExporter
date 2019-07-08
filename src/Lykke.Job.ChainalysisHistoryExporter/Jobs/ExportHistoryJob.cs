using System;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Job.ChainalysisHistoryExporter.Assets;
using Lykke.Job.ChainalysisHistoryExporter.Common;

namespace Lykke.Job.ChainalysisHistoryExporter.Jobs
{
    public class ExportHistoryJob
    {
        public const string Id = "3e4474356a444a6cae855f68a49ebe03";

        private readonly AssetsClient _assetsClient;
        private readonly Exporter _exporter;
        private readonly SemaphoreSlim _lock;

        public ExportHistoryJob(
            AssetsClient assetsClient,
            Exporter exporter)
        {
            _assetsClient = assetsClient;
            _exporter = exporter;
            _lock = new SemaphoreSlim(1);
        }

        public async Task ExecuteAsync()
        {
            if (!await _lock.WaitAsync(TimeSpan.FromMilliseconds(100)))
            {
                return;
            }

            try
            {
                await _assetsClient.LoadAssetsAsync();
                await _exporter.ExportAsync();
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}
