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

        public ExportHistoryJob(
            AssetsClient assetsClient,
            Exporter exporter)
        {
            _assetsClient = assetsClient;
            _exporter = exporter;
        }

        public async Task ExecuteAsync()
        {
            await _assetsClient.LoadAssetsAsync();
            await _exporter.ExportAsync();
        }
    }
}
