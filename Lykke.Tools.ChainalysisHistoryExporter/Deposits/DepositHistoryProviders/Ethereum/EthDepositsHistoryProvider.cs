using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Lykke.Tools.ChainalysisHistoryExporter.Deposits.DepositHistoryProviders.Ethereum
{
    public class EthDepositsHistoryProvider : IDepositsHistoryProvider
    {
        private enum ContinuationStage
        {
            Transactions,
            InternalTransactions
        }

        private class ContinuationToken
        {
            public ContinuationStage Stage { get; set; }
            public string InnerContinuation { get; set; }
        }

        private readonly SamuraiClient _samuraiClient;
        private readonly Blockchain _ethereum;

        public EthDepositsHistoryProvider(
            BlockchainsProvider blockchainsProvider,
            IOptions<EthSettings> settings)
        {
            _samuraiClient = new SamuraiClient(settings.Value.SamuraiUrl);
            _ethereum = blockchainsProvider.GetEthereum();
        }

        public bool CanProvideHistoryFor(DepositWallet depositWallet)
        {
            return _ethereum.CryptoCurrency == depositWallet.CryptoCurrency;
        }

        public async Task<PaginatedList<Transaction>> GetHistoryAsync(DepositWallet depositWallet, string continuation)
        {
            if (!CanProvideHistoryFor(depositWallet))
            {
                return PaginatedList.From(Array.Empty<Transaction>());
            }

            var continuationToken = continuation != null
                ? JsonConvert.DeserializeObject<ContinuationToken>(continuation)
                : new ContinuationToken {Stage = ContinuationStage.Transactions};

            if (continuationToken.Stage == ContinuationStage.Transactions)
            {
                var result = await ReadTransactionsAsync(depositWallet, continuationToken.InnerContinuation);

                if (result.Continuation == null)
                {
                    return PaginatedList.From(new ContinuationToken {Stage = ContinuationStage.InternalTransactions}, result.Items);
                }

                return PaginatedList.From
                (
                    new ContinuationToken
                    {
                        Stage = ContinuationStage.Transactions, 
                        InnerContinuation = result.Continuation
                    },
                    result.Items
                );
            }

            if (continuationToken.Stage == ContinuationStage.InternalTransactions)
            {
                var result = await ReadInternalTransactionsAsync(depositWallet, continuationToken.InnerContinuation);

                if (result.Continuation == null)
                {
                    return PaginatedList.From(result.Items);
                }

                return PaginatedList.From
                (
                    new ContinuationToken
                    {
                        Stage = ContinuationStage.InternalTransactions, 
                        InnerContinuation = result.Continuation
                    },
                    result.Items
                );
            }

            throw new InvalidOperationException($"Unknown continuation stage: {continuationToken.Stage}");
        }

        private async Task<PaginatedList<Transaction>> ReadTransactionsAsync(DepositWallet depositWallet, string continuation)
        {
            var address = depositWallet.Address;
            var operations = await _samuraiClient.GetOperationsHistoryAsync(depositWallet.Address, continuation);
            var transactions = operations.Items
                .Where(x => string.Equals(x.To, address, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => new Transaction
                (
                    _ethereum.CryptoCurrency,
                    x.TransactionHash,
                    depositWallet.UserId,
                    depositWallet.Address,
                    TransactionType.Deposit
                ))
                .ToArray();

            return PaginatedList.From(operations.Continuation, transactions);
        }

        private async Task<PaginatedList<Transaction>> ReadInternalTransactionsAsync(DepositWallet depositWallet, string continuation)
        {
            var address = depositWallet.Address;
            var operations = await _samuraiClient.GetErc20OperationsHistory(depositWallet.Address, continuation);
            var transactions = operations.Items
                .Where(x => string.Equals(x.To, address, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => new Transaction
                (
                    _ethereum.CryptoCurrency,
                    x.TransactionHash,
                    depositWallet.UserId,
                    depositWallet.Address,
                    TransactionType.Deposit
                ))
                .ToArray();

            return PaginatedList.From(operations.Continuation, transactions);
        }
    }
}
