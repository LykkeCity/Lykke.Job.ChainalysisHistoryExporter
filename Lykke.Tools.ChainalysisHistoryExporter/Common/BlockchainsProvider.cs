﻿using System.Collections.Generic;
using System.Linq;
using Lykke.Tools.ChainalysisHistoryExporter.Assets;

namespace Lykke.Tools.ChainalysisHistoryExporter.Common
{
    public class BlockchainsProvider
    {
        private readonly AssetsClient _assetsClient;
        private readonly Dictionary<string, Blockchain> _byBillId;
        private readonly Dictionary<string, Blockchain> _byAssetBlockchain;

        public BlockchainsProvider(AssetsClient assetsClient)
        {
            _assetsClient = assetsClient;

            var blockchains = new List<Blockchain>
            {
                new Blockchain {CryptoCurrency = "BTC", BilId = "Bitcoin"},
                new Blockchain {CryptoCurrency = "ETH", BilId = "Ethereum", AssetBlockchain = "Ethereum"},
                new Blockchain {CryptoCurrency = "LTC", BilId = "LiteCoin"},
                new Blockchain {CryptoCurrency = "BCH", BilId = "BitcoinCash"}
            };

            _byBillId = blockchains.ToDictionary(x => x.BilId);
            _byAssetBlockchain = blockchains.Where(x => x.AssetBlockchain != null).ToDictionary(x => x.AssetBlockchain);
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
    }
}
