using System;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Assets;
using Lykke.Job.ChainalysisHistoryExporter.Common;

namespace Lykke.Job.ChainalysisHistoryExporter.Jobs
{
    public class ExportHistoryJob
    {
        public const string Id = "3e4474356a444a6cae855f68a49ebe03";

        private static readonly SemaphoreSlim Lock;

        private readonly ILog _log;
        private readonly AssetsClient _assetsClient;
        private readonly Exporter _exporter;

        static ExportHistoryJob()
        {
            Lock = new SemaphoreSlim(1);
        }

        public ExportHistoryJob(
            ILogFactory logFactory,
            AssetsClient assetsClient,
            Exporter exporter)
        {
            _log = logFactory.CreateLog(this);
            _assetsClient = assetsClient;
            _exporter = exporter;
        }

        public async Task ExecuteAsync()
        {
            if (!await Lock.WaitAsync(TimeSpan.FromMilliseconds(100)))
            {
                _log.Info("Job execution has been skipped because previous job is still executing");
                return;
            }

            try
            {
                _log.Info("Job execution has been started");

                await _assetsClient.LoadAssetsAsync();
                await _exporter.ExportAsync();
            }
            finally
            {
                Lock.Release();

                _log.Info("Job execution has been finished");
            }
        }
    }
}
