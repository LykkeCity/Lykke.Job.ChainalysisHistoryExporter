using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Lykke.Tools.ChainalysisHistoryExporter.Withdrawals.WithdrawalHistoryProviders
{
    internal class BilCashoutsBatchWithdrawalsHistoryProvider : IWithdrawalsHistoryProvider
    {
        #region Entities

        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        // ReSharper disable ClassNeverInstantiated.Local

        private static class CashoutsBatchState
        {
            public const string Finished = "Finished";
        }

        private class CashoutBatchEntity : TableEntity
        {
            public Guid BatchId { get; set; }
            public string BlockchainType { get; set; }
            public string Cashouts { get; set; }
            public string State { get; set; }
        }

        private class BatchedCashout
        {
            public Guid OperationId { get; set; }
            public Guid ClientId { get; set; }
            public string DestinationAddress { get; set; }
            public decimal Amount { get; set; }
            public int IndexInBatch { get; set; }
            public DateTime AddedToBatchAt { get; set; }
        }

        private static class OperationExecutionResult
        {
            public const string Completed = "Completed";
            public const string Success = "Success";
        }

        private class OperationExecutionEntity : TableEntity
        {
            public string State { get; set; }
            public string Result { get; set; }
            public string TransactionHash { get; set; }
        }

        // ReSharper restore UnusedMember.Local
        // ReSharper restore UnusedAutoPropertyAccessor.Local
        // ReSharper restore ClassNeverInstantiated.Local


        #endregion

        private readonly BlockchainsProvider _blockchainsProvider;
        private readonly CloudTable _cashoutBatchesTable;
        private readonly CloudTable _operationExecutionsTable;

        public BilCashoutsBatchWithdrawalsHistoryProvider(
            BlockchainsProvider blockchainsProvider,
            IOptions<AzureStorageSettings> azureStorageSettings)
        {
            _blockchainsProvider = blockchainsProvider;

            var cashoutProcessorAzureAccount = CloudStorageAccount.Parse(azureStorageSettings.Value.CashoutProcessorConnString);
            var cashoutProcessorAzureClient = cashoutProcessorAzureAccount.CreateCloudTableClient();

            _cashoutBatchesTable = cashoutProcessorAzureClient.GetTableReference("CashoutsBatch");

            var operationsExecutorAzureAccount = CloudStorageAccount.Parse(azureStorageSettings.Value.OperationsExecutorConnString);
            var operationsExecutorAzureClient = operationsExecutorAzureAccount.CreateCloudTableClient();

            _operationExecutionsTable = operationsExecutorAzureClient.GetTableReference("OperationExecutions");
        }

        public async Task<PaginatedList<Transaction>> GetHistoryAsync(string continuation)
        {
            var continuationToken = continuation != null
                ? JsonConvert.DeserializeObject<TableContinuationToken>(continuation)
                : null;
            var query = new TableQuery<CashoutBatchEntity>
            {
                TakeCount = 1000
            };
            var response = await _cashoutBatchesTable.ExecuteQuerySegmentedAsync(query, continuationToken);

            var transactions = response.Results
                .Where(cashoutsBatch => cashoutsBatch.State == CashoutsBatchState.Finished)
                .SelectMany(cashoutsBatch =>
                {
                    var blockchain = _blockchainsProvider.GetByBilIdOrDefault(cashoutsBatch.BlockchainType);

                    if (blockchain == null)
                    {
                        return Enumerable.Empty<Transaction>();
                    }

                    var operationExecution = GetOperationExecution(cashoutsBatch.BatchId).Result;

                    if (operationExecution.Result != OperationExecutionResult.Completed &&
                        operationExecution.Result != OperationExecutionResult.Success ||
                        operationExecution.TransactionHash == null)
                    {
                        return Enumerable.Empty<Transaction>();
                    }

                    var cashouts = JsonConvert.DeserializeObject<BatchedCashout[]>(cashoutsBatch.Cashouts);

                    return cashouts.Select(cashout => new Transaction
                    {
                        CryptoCurrency = blockchain.CryptoCurrency,
                        Hash = operationExecution.TransactionHash,
                        UserId = cashout.ClientId,
                        OutputAddress = cashout.DestinationAddress,
                        Type = TransactionType.Withdrawal
                    });
                })
                .ToArray();

            return PaginatedList.From(response.ContinuationToken, transactions);
        }

        // TODO: This produces a lot of round trips. Need to read all operations at once and cache them
        private async Task<OperationExecutionEntity> GetOperationExecution(Guid batchId)
        {
            var partitionKey = CalculateHexHash32(batchId.ToString());
            var rowKey = $"{batchId:D}";
            var tableOperation = TableOperation.Retrieve<OperationExecutionEntity>(partitionKey, rowKey);

            var response = await _operationExecutionsTable.ExecuteAsync(tableOperation);

            return (OperationExecutionEntity) response.Result;
        }

        private static string CalculateHexHash32(string value, int length = 3)
        {
            if (length < 1 || length > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "Length should be in the range [1..8]");
            }

            uint mask = 0xF;

            for (var i = 1; i < length; ++i)
            {
                // One hex digit - 4 bits, so multiplies i by 4
                mask |= 0xFu << (i * 4);
            }

            unchecked
            {
                return ((uint)CalculateHash32(value) & mask).ToString($"X{length}");
            }
        }

        private static int CalculateHash32(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var hashedValue = 618258791u;

            foreach (var c in value)
            {
                unchecked
                {
                    hashedValue += c;
                    hashedValue *= 618258799u;
                }
            }

            unchecked
            {
                return (int)hashedValue;
            }
        }
    }
}