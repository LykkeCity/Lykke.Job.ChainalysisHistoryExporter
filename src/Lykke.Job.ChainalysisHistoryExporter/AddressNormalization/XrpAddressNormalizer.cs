using System;
using Lykke.Job.ChainalysisHistoryExporter.Common;

namespace Lykke.Job.ChainalysisHistoryExporter.AddressNormalization
{
    public class XrpAddressNormalizer : IAddressNormalizer
    {
        private readonly Blockchain _ripple;

        public XrpAddressNormalizer(IBlockchainsProvider blockchainsProvider)
        {
            _ripple = blockchainsProvider.GetRipple();
        }

        public bool CanNormalize(string cryptoCurrency)
        {
            return _ripple.CryptoCurrency == cryptoCurrency;
        }

        public string NormalizeOrDefault(string address)
        {
            var addressParts = address.Split('+', StringSplitOptions.RemoveEmptyEntries);
            var adr = addressParts[0];
            var tag = addressParts.Length > 1
                ? addressParts[1]
                : null;

            if (!Ripple.Address.AddressCodec.IsValidAddress(adr))
            {
                return null;
            }

            if (!string.IsNullOrEmpty(tag) && (!uint.TryParse(tag, out var tagValue) || tagValue == 0))
            {
                return null;
            }

            // consumer is interested in real blockchain address only,
            // so instead of deposit wallet address in form "{address}+{tag}"
            // return just Ripple address without tag
            return adr;
        }
    }
}
