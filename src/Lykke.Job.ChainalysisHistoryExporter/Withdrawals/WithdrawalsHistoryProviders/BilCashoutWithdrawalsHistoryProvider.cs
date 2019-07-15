using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.Withdrawals.WithdrawalsHistoryProviders
{
    public class BilCashoutWithdrawalsHistoryProvider : IWithdrawalsHistoryProvider
    {
        #region Entities

        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        // ReSharper disable ClassNeverInstantiated.Local

        private static class CashoutResult
        {
            public const string Unknown = "Unknown";
            public const string Success = "Success";
            public const string Failure = "Failure";
        }

        private class CashoutEntity : TableEntity
        {
            public string State { get; set; }
            public string Result { get; set; }
            public Guid ClientId { get; set; }
            public string BlockchainType { get; set; }
            public string ToAddress { get; set; }
            public string TransactionHash { get; set; }
        }

        // ReSharper restore UnusedMember.Local
        // ReSharper restore UnusedAutoPropertyAccessor.Local
        // ReSharper restore ClassNeverInstantiated.Local

        #endregion

        private readonly IBlockchainsProvider _blockchainsProvider;
        private readonly CloudTable _table;
        

        public BilCashoutWithdrawalsHistoryProvider(
            IBlockchainsProvider blockchainsProvider,
            AzureStorageSettings azureStorageSettings)
        {
            _blockchainsProvider = blockchainsProvider;

            var azureAccount = CloudStorageAccount.Parse(azureStorageSettings.CashoutProcessorConnString);
            var azureClient = azureAccount.CreateCloudTableClient();

            _table = azureClient.GetTableReference("Cashout");
        }

        public async Task<PaginatedList<Transaction>> GetHistoryAsync(string continuation)
        {
            var continuationToken = continuation != null
                ? JsonConvert.DeserializeObject<TableContinuationToken>(continuation)
                : null;
            var query = new TableQuery<CashoutEntity>
            {
                TakeCount = 1000
            };
            var response = await _table.ExecuteQuerySegmentedAsync(query, continuationToken);

            var transactions = response.Results
                .Where(cashout => cashout.Result == CashoutResult.Success && !string.IsNullOrWhiteSpace(cashout.TransactionHash))
                .Select(cashout =>
                {
                    var blockchain = _blockchainsProvider.GetByBilIdOrDefault(cashout.BlockchainType);

                    if (blockchain == null)
                    {
                        return null;
                    }

                    return new Transaction
                    (
                        blockchain.CryptoCurrency,
                        cashout.TransactionHash,
                        cashout.ClientId,
                        cashout.ToAddress,
                        TransactionType.Withdrawal
                    );
                })
                .Where(x => x != null)
                .ToArray();

            return PaginatedList.From(response.ContinuationToken, transactions);
        }
    }
}
