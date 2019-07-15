using System;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using NBitcoin;
using NBitcoin.Altcoins;

namespace Lykke.Job.ChainalysisHistoryExporter.AddressNormalization
{
    public class BchAddressNormalizer : IAddressNormalizer
    {
        private readonly Blockchain _bitcoinCash;
        private readonly Network _btcNetwork;
        private readonly Network _bchNetwork;

        public BchAddressNormalizer(
            IBlockchainsProvider blockchainsProvider,
            BchSettings settings)
        {
            _bitcoinCash = blockchainsProvider.GetBitcoinCash();
            _btcNetwork = Network.GetNetwork(settings.Network);
            _bchNetwork = _btcNetwork == Network.Main ? BCash.Instance.Mainnet : BCash.Instance.Regtest;
        }

        public bool CanNormalize(string cryptoCurrency)
        {
            return _bitcoinCash.CryptoCurrency == cryptoCurrency;
        }

        public string NormalizeOrDefault(string address)
        {
            // ReSharper disable CommentTypo
            // eg: moc231tgxApbRSwLNrc9ZbSVDktTRo3acK
            var legacyAddress = GetBitcoinAddress(address, _btcNetwork);
            if (legacyAddress != null)
            {
                return legacyAddress.ScriptPubKey.GetDestinationAddress(_bchNetwork).ToString();
            }

            // eg: bitcoincash:qpm2qsznhks23z7629mms6s4cwef74vcwvy22gdx6a
            var canonicalAddress = GetBitcoinAddress(address, _bchNetwork);
            if (canonicalAddress != null)
            {
                return canonicalAddress.ToString();
            }
            
            // eg: qpm2qsznhks23z7629mms6s4cwef74vcwvy22gdx6a
            // ReSharper restore CommentTypo
            var addressWithoutPrefix = GetBitcoinAddress($"{GetAddressPrefix(_bchNetwork)}:{address}", _bchNetwork);

            return addressWithoutPrefix?.ToString();
        }

        private static BitcoinAddress GetBitcoinAddress(string address, Network network)
        {
            try
            {
                return BitcoinAddress.Create(address, network);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetAddressPrefix(Network bchNetwork)
        {
            if (bchNetwork == BCash.Instance.Mainnet)
            {
                return "bitcoincash";
            }
            if (bchNetwork == BCash.Instance.Regtest)
            {
                return "bchreg";
            }
            if (bchNetwork == BCash.Instance.Testnet)
            {
                return "bchtest";
            }

            throw new ArgumentException("Unknown Bitcoin Cash network", nameof(bchNetwork));
        }
    }
}
