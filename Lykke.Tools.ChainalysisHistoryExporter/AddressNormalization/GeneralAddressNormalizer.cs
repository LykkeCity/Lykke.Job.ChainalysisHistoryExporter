﻿using System;

namespace Lykke.Tools.ChainalysisHistoryExporter.AddressNormalization
{
    public class GeneralAddressNormalizer : IAddressNormalizer
    {
        public bool CanNormalize(string cryptoCurrency)
        {
            return true;
        }

        public string NormalizeOrDefault(string address)
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
