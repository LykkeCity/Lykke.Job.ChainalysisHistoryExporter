using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Assets;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Sdk;

namespace Lykke.Job.ChainalysisHistoryExporter.Services
{
    // NOTE: Sometimes, startup process which is expressed explicitly is not just better, 
    // but the only way. If this is your case, use this class to manage startup.
    // For example, sometimes some state should be restored before any periodical handler will be started, 
    // or any incoming message will be processed and so on.
    // Do not forget to remove As<IStartable>() and AutoActivate() from DI registartions of services, 
    // which you want to startup explicitly.

    public class StartupManager : IStartupManager
    {
        private readonly ILog _log;
        private readonly Exporter _exporter;
        private readonly AssetsClient _assetsClient;

        public StartupManager(
            ILogFactory logFactory,
            Exporter exporter,
            AssetsClient assetsClient)
        {
            _log = logFactory.CreateLog(this);
            _exporter = exporter;
            _assetsClient = assetsClient;
        }

        public async Task StartAsync()
        {
            await _assetsClient.InitializeAsync();
            await _exporter.ExportAsync();
        }
    }
}
