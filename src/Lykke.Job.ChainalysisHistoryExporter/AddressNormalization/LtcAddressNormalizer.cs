using System;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using NBitcoin;

namespace Lykke.Job.ChainalysisHistoryExporter.AddressNormalization
{
    public class LtcAddressNormalizer : IAddressNormalizer
    {
        private readonly Blockchain _liteCoin;
        private readonly Network _network;

        public LtcAddressNormalizer(
            IBlockchainsProvider blockchainsProvider,
            LtcSettings settings)
        {
            _liteCoin = blockchainsProvider.GetLiteCoin();
            _network = Network.GetNetwork(settings.Network);
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
