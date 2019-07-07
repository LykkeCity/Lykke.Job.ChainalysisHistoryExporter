using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using NBitcoin;
using QBitNinja.Client.Models;
using Transaction = Lykke.Job.ChainalysisHistoryExporter.Reporting.Transaction;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.Bitcoin
{
    public class BtcDepositsHistoryProvider : IDepositsHistoryProvider
    {
        private readonly CustomQBitNinjaClient _client;
        private readonly Blockchain _bitcoin;

        public BtcDepositsHistoryProvider(
            BlockchainsProvider blockchainsProvider,
            BtcSettings settings)
        {
            _bitcoin = blockchainsProvider.GetBitcoin();
            _client = new CustomQBitNinjaClient(new Uri(settings.NinjaUrl), Network.GetNetwork(settings.Network));
        }

        public bool CanProvideHistoryFor(DepositWallet depositWallet)
        {
            return depositWallet.CryptoCurrency == _bitcoin.CryptoCurrency;
        }

        public async Task<PaginatedList<Transaction>> GetHistoryAsync(DepositWallet depositWallet, string continuation)
        {
            if (!CanProvideHistoryFor(depositWallet))
            {
                return PaginatedList.From(Array.Empty<Transaction>());
            }

            var response = await _client.GetBalance(depositWallet.Address, false, continuation);
            var depositOperations = response.Operations.Where(IsDeposit);
            var depositTransactions = Map(depositOperations, depositWallet.Address, depositWallet.UserId);

            return PaginatedList.From(response.Continuation, depositTransactions.ToArray());
        }

        private IEnumerable<Transaction> Map(IEnumerable<BalanceOperation> source, 
            string outputAddress,
            Guid userId)
        {
            return source.Select(balanceOperation => new Transaction
            (
                _bitcoin.CryptoCurrency,
                balanceOperation.TransactionId.ToString(),
                userId,
                outputAddress,
                TransactionType.Deposit
            ));
        }

        private static bool IsDeposit(BalanceOperation source)
        {
            return !source.SpentCoins.Any();
        }
    }
}
