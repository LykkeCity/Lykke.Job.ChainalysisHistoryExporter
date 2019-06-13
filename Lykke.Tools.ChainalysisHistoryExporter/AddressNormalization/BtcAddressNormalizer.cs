using System;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Microsoft.Extensions.Options;
using NBitcoin;

namespace Lykke.Tools.ChainalysisHistoryExporter.AddressNormalization
{
    public class BtcAddressNormalizer : IAddressNormalizer
    {
        private readonly Blockchain _bitcoin;
        private readonly Network _network;

        public BtcAddressNormalizer(
            BlockchainsProvider blockchainsProvider,
            IOptions<BtcSettings> settings)
        {
            _bitcoin = blockchainsProvider.GetBitcoin();
            _network = Network.GetNetwork(settings.Value.Network);
        }

        public bool CanNormalize(string cryptoCurrency)
        {
            return _bitcoin.CryptoCurrency == cryptoCurrency;
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
                try
                {
                    var coloredAddress = new BitcoinColoredAddress(address, _network);
                    var bitcoinAddress = coloredAddress.ScriptPubKey.GetDestinationAddress(_network);

                    return bitcoinAddress.ToString();
                }
                catch (FormatException)
                {
                    return null;
                }
            }
        }
    }
}
