using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lykke.Tools.ChainalysisHistoryExporter.Assets
{
    internal class AssetsProvider
    {
        private readonly ILogger<AssetsProvider> _logger;
        private readonly IOptions<ServicesSettings> _settings;
        private Dictionary<string, Asset> _assets;

        public AssetsProvider(
            ILogger<AssetsProvider> logger, 
            IOptions<ServicesSettings> settings)
        {
            _logger = logger;
            _settings = settings;
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("Loading assets...");

            var response = await _settings.Value.Assets
                .AppendPathSegments("api", "v2", "assets")
                .SetQueryParams(new {includeNonTradable = true})
                .GetJsonAsync<Asset[]>();

            _assets = response.ToDictionary(x => x.Id);

            _logger.LogInformation($"Assets loading done. {_assets.Count} assets loaded.");
        }

        public Asset GetByIdOrDefault(string id)
        {
            _assets.TryGetValue(id, out var asset);

            return asset;
        }
    }
}
