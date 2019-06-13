using System;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Microsoft.Extensions.Options;
using NBitcoin;

namespace Lykke.Tools.ChainalysisHistoryExporter.AddressNormalization
{
    public class LtcAddressNormalizer : IAddressNormalizer
    {
        private readonly Blockchain _liteCoin;
        private readonly Network _network;

        public LtcAddressNormalizer(
            BlockchainsProvider blockchainsProvider,
            IOptions<LtcSettings> settings)
        {
            _liteCoin = blockchainsProvider.GetLiteCoin();
            _network = Network.GetNetwork(settings.Value.Network);
        }

        public bool CanNormalize(string cryptoCurrency)
        {
            return _liteCoin.CryptoCurrency == cryptoCurrency;
        }

        public string NormalizeOrDefault(string address)
        {
            try
            {
                var bitcoinAddress = BitcoinAddress.Create(address, _network);

                return bitcoinAddress.ToString();
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
