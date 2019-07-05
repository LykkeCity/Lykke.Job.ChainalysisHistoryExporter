using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Flurl;
using Flurl.Http;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Settings;

namespace Lykke.Job.ChainalysisHistoryExporter.Assets
{
    // TODO: Replace with standard assets service client
    public class AssetsClient
    {
        private readonly ILog _log;
        private readonly string _url;

        private Dictionary<string, Asset> _assets;
        
        public AssetsClient(
            ILogFactory logFactory,
            AssetsClientSettings settings)
        {
            _url = settings.ServiceUrl;
            _log = logFactory.CreateLog(this);
        }

        public async Task InitializeAsync()
        {
            _log.Info("Loading assets...");

            var response = await _url
                .AppendPathSegments("api", "v2", "assets")
                .SetQueryParams(new {includeNonTradable = true})
                .GetJsonAsync<Asset[]>();

            _assets = response.ToDictionary(x => x.Id);

            _log.Info($"Assets loading done. {_assets.Count} assets loaded.");
        }

        public Asset GetByIdOrDefault(string id)
        {
            _assets.TryGetValue(id, out var asset);

            return asset;
        }
    }
}
