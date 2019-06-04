using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;
using Microsoft.Extensions.Options;
using NBitcoin;
using QBitNinja.Client.Models;
using Transaction = Lykke.Tools.ChainalysisHistoryExporter.Reporting.Transaction;

namespace Lykke.Tools.ChainalysisHistoryExporter.Deposits.DepositHistoryProviders
{
    internal class BtcDepositsHistoryProvider : IDepositsHistoryProvider
    {
        private readonly CustomQBitNinjaClient _client;
        
        public BtcDepositsHistoryProvider(IOptions<BtcSettings> settings)
        {
            _client = new CustomQBitNinjaClient(new Uri(settings.Value.NinjaUrl), Network.GetNetwork(settings.Value.Network));
        }

        public async Task<PaginatedList<Transaction>> GetHistoryAsync(DepositWallet depositWallet, string continuation)
        {
            if (depositWallet.CryptoCurrency != "BTC")
            {
                return PaginatedList.From(Array.Empty<Transaction>());
            }

            var btcAddr = GetAddressOrDefault(depositWallet.Address);
            if (btcAddr == null)
            {
                return PaginatedList.From(Array.Empty<Transaction>());
            }

            var resp = await _client.GetBalance(btcAddr, false, continuation);

            var mapped = Map(resp.Operations.Where(IsCashin).ToList(), depositWallet.Address, depositWallet.UserId);

            return PaginatedList.From(resp.Continuation, mapped.ToArray());
        }

        private static IEnumerable<Transaction> Map(IEnumerable<BalanceOperation> source, 
            string outputAddress,
            Guid depositWalletUserId)
        {
            foreach (var balanceOperation in source)
            {
                yield return new Transaction
                {
                    CryptoCurrency = "BTC",
                    Hash = balanceOperation.TransactionId.ToString(),
                    OutputAddress = outputAddress,
                    Type = TransactionType.Deposit,
                    UserId = depositWalletUserId
                };
            }
        }

        private static bool IsCashin(BalanceOperation source)
        {
            return !source.SpentCoins.Any();
        }

        private BitcoinAddress GetAddressOrDefault(string address)
        {

            if (IsUncoloredBtcAddress(address))
            {
                return BitcoinAddress.Create(address, _client.Network);
            }

            if (IsColoredBtcAddress(address))
            {
                return new BitcoinColoredAddress(address, _client.Network).Address;
            }

            return null;
        }

        private bool IsUncoloredBtcAddress(string address)
        {
            try
            {
                BitcoinAddress.Create(address,
                    _client.Network);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private bool IsColoredBtcAddress(string address)
        {
            try
            {
                // ReSharper disable once ObjectCreationAsStatement
                new BitcoinColoredAddress(address, _client.Network);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
