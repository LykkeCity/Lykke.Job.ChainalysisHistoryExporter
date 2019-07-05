using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Job.ChainalysisHistoryExporter.AddressNormalization;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Lykke.Job.ChainalysisHistoryExporter.Settings;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositHistoryProviders.Ethereum
{
    public class EthDepositsHistoryProvider : IDepositsHistoryProvider
    {
        private readonly SamuraiClient _samuraiClient;
        private readonly Blockchain _ethereum;
        private readonly AddressNormalizer _addressNormalizer;

        public EthDepositsHistoryProvider(
            BlockchainsProvider blockchainsProvider,
            EthSettings settings,
            AddressNormalizer addressNormalizer)
        {
            _addressNormalizer = addressNormalizer;
            _samuraiClient = new SamuraiClient(settings.SamuraiUrl);
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

            if (continuation != null)
            {
                throw new NotSupportedException("Continuation is not supported");
            }

            var operations = await ReadTransactionsAsync(depositWallet);
            var internalOperations = await ReadInternalTransactionsAsync(depositWallet);
            var internalOperationHashes = internalOperations.Select(x => x.TransactionHash).ToHashSet();
            var transactions = operations
                .Where(x => !internalOperationHashes.Contains(x.TransactionHash) && IsDeposit(depositWallet.Address, x.To))
                .Select(x => new Transaction
                (
                    _ethereum.CryptoCurrency,
                    x.TransactionHash.ToLower(),
                    depositWallet.UserId,
                    depositWallet.Address,
                    TransactionType.Deposit
                ));
            var internalTransactions = internalOperations
                .Where(x => IsDeposit(depositWallet.Address, x.To))
                .Select(x => new Transaction
                (
                    _ethereum.CryptoCurrency,
                    x.TransactionHash.ToLower(),
                    depositWallet.UserId,
                    depositWallet.Address,
                    TransactionType.Deposit
                ));
            var allTransactions = transactions.Concat(internalTransactions).ToArray();

            return PaginatedList.From(allTransactions);
        }

        private bool IsDeposit(string depositWalletAddress, string operationToAddress)
        {
            return string.Equals
            (
                _addressNormalizer.NormalizeOrDefault(operationToAddress, _ethereum.CryptoCurrency),
                depositWalletAddress,
                StringComparison.InvariantCultureIgnoreCase
            );
        }

        private async Task<IReadOnlyCollection<SamuraiOperation>> ReadTransactionsAsync(DepositWallet depositWallet)
        {
            var operations = default(PaginatedList<SamuraiOperation>);
            var allOperations = new List<SamuraiOperation>();

            do
            {
                operations = await _samuraiClient.GetOperationsHistoryAsync(depositWallet.Address, operations?.Continuation);
                allOperations.AddRange(operations.Items);
            } while (operations.Continuation != null);

            return allOperations;
        }

        private async Task<IReadOnlyCollection<SamuraiErc20Operation>> ReadInternalTransactionsAsync(DepositWallet depositWallet)
        {
            var operations = default(PaginatedList<SamuraiErc20Operation>);
            var allOperations = new List<SamuraiErc20Operation>();

            do
            {
                operations = await _samuraiClient.GetErc20OperationsHistory(depositWallet.Address, operations?.Continuation);
                allOperations.AddRange(operations.Items);
            } while (operations.Continuation != null);

            return allOperations;
        }
    }
}
