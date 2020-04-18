using System;

namespace Lykke.Job.ChainalysisHistoryExporter.AddressNormalization
{
    public class GeneralAddressNormalizer : IAddressNormalizer
    {
        public bool CanNormalize(string cryptoCurrency)
        {
            return true;
        }

        public string NormalizeOrDefault(string address, bool isTransactionNormalization = false)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                return null;
            }

            var parts = address.Split(new[] {'?'}, StringSplitOptions.RemoveEmptyEntries);

            return parts[0].Trim();
        }
    }
}
