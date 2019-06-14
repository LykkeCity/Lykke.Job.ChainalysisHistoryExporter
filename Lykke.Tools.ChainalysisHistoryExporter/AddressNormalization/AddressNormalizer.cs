using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Lykke.Tools.ChainalysisHistoryExporter.AddressNormalization
{
    public class AddressNormalizer
    {
        private readonly IReadOnlyCollection<IAddressNormalizer> _normalizers;

        public AddressNormalizer(
            ILogger<AddressNormalizer> logger,
            IEnumerable<IAddressNormalizer> normalizers)
        {
            _normalizers = normalizers.ToArray();

            logger.LogInformation($"Address normalizers: {string.Join(", ", _normalizers.Select(x => x.GetType().Name))}");
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
