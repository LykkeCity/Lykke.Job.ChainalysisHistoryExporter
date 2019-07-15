using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Multiformats.Hash;
using Multiformats.Hash.Algorithms;

namespace Lykke.Job.ChainalysisHistoryExporter.AddressNormalization
{
    public class EthAddressNormalizer : IAddressNormalizer
    {
        private static readonly Regex AddressStringExpression = new Regex(@"^0x[0-9a-fA-F]{40}$", RegexOptions.Compiled);

        private readonly Blockchain _ethereum;

        public EthAddressNormalizer(IBlockchainsProvider blockchainsProvider)
        {
            _ethereum = blockchainsProvider.GetEthereum();
        }

        public bool CanNormalize(string cryptoCurrency)
        {
            return _ethereum.CryptoCurrency == cryptoCurrency;
        }

        public string NormalizeOrDefault(string address)
        {
            return NormalizeOrDefaultInternal(address) ?? NormalizeOrDefaultInternal($"0x{address}");
        }

        private static string NormalizeOrDefaultInternal(string address)
        {
            if (!AddressStringExpression.IsMatch(address))
            {
                return null;
            }

            if (address.Skip(2).Where(char.IsLetter).All(char.IsLower))
            {
                return address;
            }

            if (address.Skip(2).Where(char.IsLetter).All(char.IsUpper))
            {
                return address;
            }

            if (ValidateChecksum(address))
            {
                return address;
            }

            return null;
        }

        private static bool ValidateChecksum(
            string addressString)
        {
            addressString = addressString.Remove(0, 2);
            
            var addressBytes = Encoding.UTF8.GetBytes(addressString.ToLowerInvariant());
            var caseMapBytes = SumBytes(addressBytes);
        
            for (var i = 0; i < 40; i++)
            {
                var addressChar = addressString[i];
        
                if (!char.IsLetter(addressChar))
                {
                    continue;
                }
        
                var leftShift = i % 2 == 0 ? 7 : 3;
                var shouldBeUpper = (caseMapBytes[i / 2] & (1 << leftShift)) != 0;
                var shouldBeLower = !shouldBeUpper;
        
                if (shouldBeUpper && char.IsLower(addressChar) ||
                    shouldBeLower && char.IsUpper(addressChar))
                {
                    return false;
                }
            }
        
            return true;
        }

        private static byte[] SumBytes(params byte[][] data)
        {
            var multihash = Multihash.Sum<KECCAK_256>
            (
                data: ConcatMany(data)
            );

            return multihash.Digest;
        }

        private static byte[] ConcatMany(IEnumerable<byte[]> data)
        {
            return data
                .SelectMany(x => x)
                .ToArray();
        }
    }
}
