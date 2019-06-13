using System.Collections.Generic;
using System.Linq;

namespace Lykke.Tools.ChainalysisHistoryExporter.AddressNormalization
{
    public class AddressNormalizer
    {
        private readonly IReadOnlyCollection<IAddressNormalizer> _normalizers;

        public AddressNormalizer(IEnumerable<IAddressNormalizer> normalizers)
        {
            _normalizers = normalizers.ToArray();
        }

        public string NormalizeOrDefault(string address, string cryptoCurrency)
        {
            var currentAddress = address;

            foreach (var normalizer in _normalizers.Where(x => x.CanNormalize(cryptoCurrency)))
            {
                currentAddress = normalizer.NormalizeOrDefault(currentAddress);

                if (currentAddress == null)
                {
                    return null;
                }
            }

            return currentAddress;
        }
    }
}
