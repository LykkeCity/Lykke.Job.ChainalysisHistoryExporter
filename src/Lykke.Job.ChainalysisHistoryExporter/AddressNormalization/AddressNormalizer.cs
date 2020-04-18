using System.Collections.Generic;
using System.Linq;
using Lykke.Common.Log;

namespace Lykke.Job.ChainalysisHistoryExporter.AddressNormalization
{
    public class AddressNormalizer
    {
        private readonly IReadOnlyCollection<IAddressNormalizer> _normalizers;

        public AddressNormalizer(
            ILogFactory logFactory,
            IEnumerable<IAddressNormalizer> normalizers)
        {
            _normalizers = normalizers.ToArray();

            var log = logFactory.CreateLog(this);

            log.Info("Address normalizer created", new
            {
                AddressNormalizers = _normalizers.Select(x => x.GetType().Name)
            });
        }

        public string NormalizeOrDefault(string address, string cryptoCurrency, bool isTransactionNormalization = false)
        {
            var currentAddress = address;

            foreach (var normalizer in _normalizers.Where(x => x.CanNormalize(cryptoCurrency)))
            {
                currentAddress = normalizer.NormalizeOrDefault(currentAddress, isTransactionNormalization);

                if (currentAddress == null)
                {
                    return null;
                }
            }

            return currentAddress;
        }
    }
}
