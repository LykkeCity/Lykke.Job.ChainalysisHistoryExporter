using System.Collections.Generic;
using System.Linq;
using Lykke.Job.ChainalysisHistoryExporter.Assets;

namespace Lykke.Job.ChainalysisHistoryExporter.Common
{
    public class BlockchainsProvider : IBlockchainsProvider
    {
        private readonly AssetsClient _assetsClient;
        private readonly Dictionary<string, Blockchain> _byBillId;
        private readonly Dictionary<string, Blockchain> _byAssetBlockchain;
        private readonly Dictionary<string, Blockchain> _byAssetReferences;

        public BlockchainsProvider(AssetsClient assetsClient)
        {
            _assetsClient = assetsClient;

            var blockchains = new List<Blockchain>
            {
                new Blockchain {CryptoCurrency = "BTC", BilId = "Bitcoin"},
                new Blockchain {CryptoCurrency = "ETH", BilId = "Ethereum", AssetBlockchain = "Ethereum", AssetReferences = new [] { "ERC20", "ERC223" }},
                new Blockchain {CryptoCurrency = "LTC", BilId = "LiteCoin"},
                new Blockchain {CryptoCurrency = "BCH", BilId = "BitcoinCash"}
            };

            _byBillId = blockchains.ToDictionary(x => x.BilId);
            _byAssetBlockchain = blockchains.Where(x => x.AssetBlockchain != null).ToDictionary(x => x.AssetBlockchain);
            _byAssetReferences = blockchains
                .Where(x => x.AssetReferences != null)
                .SelectMany(x => x.AssetReferences.Select(a => new {AssetId = a, Blockchain = x}))
                .ToDictionary(x => x.AssetId, x => x.Blockchain);
        }

        public Blockchain GetByBilIdOrDefault(string bilId)
        {
            _byBillId.TryGetValue(bilId, out var blockchain);

            return blockchain;
        }

        public Blockchain GetBitcoin()
        {
            return GetByBilIdOrDefault("Bitcoin");
        }

        public Blockchain GetEthereum()
        {
            return GetByBilIdOrDefault("Ethereum");
        }

        public Blockchain GetLiteCoin()
        {
            return GetByBilIdOrDefault("LiteCoin");
        }

        public Blockchain GetBitcoinCash()
        {
            return GetByBilIdOrDefault("BitcoinCash");
        }

        public Blockchain GetByAssetIdOrDefault(string assetId)
        {
            var asset = _assetsClient.GetByIdOrDefault(assetId);

            if (asset == null)
            {
                return null;
            }

            if (asset.BlockchainIntegrationLayerId != null)
            {
                var result = GetByBilIdOrDefault(asset.BlockchainIntegrationLayerId);

                return result;
            }

            if (asset.Blockchain != null)
            {
                var result = GetByAssetBlockchainOrDefault(asset.Blockchain);

                return result;
            }

            return null;
        }

        private Blockchain GetByAssetBlockchainOrDefault(string assetBlockchain)
        {
            _byAssetBlockchain.TryGetValue(assetBlockchain, out var blockchain);

            return blockchain;
        }

        public Blockchain GuessBlockchainOrDefault(string assetReference)
        {
            if (assetReference == null)
            {
                return null;
            }

            var blockchain = GetByBilIdOrDefault(assetReference);
            if (blockchain != null)
            {
                return blockchain;
            }

            _byAssetReferences.TryGetValue(assetReference, out blockchain);
            if (blockchain != null)
            {
                return blockchain;
            }

            return GetByAssetIdOrDefault(assetReference);
        }
    }
}
