using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Flurl;
using Flurl.Http;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Configuration;
using Microsoft.Extensions.Options;

namespace Lykke.Job.ChainalysisHistoryExporter.Assets
{
    public class AssetsClient
    {
        private readonly ILog _log;
        private readonly IOptions<ServicesSettings> _settings;
        private Dictionary<string, Asset> _assets;
        
        public AssetsClient(
            ILogFactory logFactory,
            IOptions<ServicesSettings> settings)
        {
            _log = logFactory.CreateLog(this);
            _settings = settings;
        }

        public async Task InitializeAsync()
        {
            _log.Info("Loading assets...");

            var response = await _settings.Value.Assets
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
