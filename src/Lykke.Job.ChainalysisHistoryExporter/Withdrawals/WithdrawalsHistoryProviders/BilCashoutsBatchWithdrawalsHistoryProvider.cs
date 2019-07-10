using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.Withdrawals.WithdrawalsHistoryProviders
{
    public class BilCashoutsBatchWithdrawalsHistoryProvider : IWithdrawalsHistoryProvider
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
            public Guid OperationId { get; set; }
            public string State { get; set; }
            public string Result { get; set; }
            public string TransactionHash { get; set; }
        }

        // ReSharper restore UnusedMember.Local
        // ReSharper restore UnusedAutoPropertyAccessor.Local
        // ReSharper restore ClassNeverInstantiated.Local


        #endregion

        private readonly ILog _log;
        private readonly BlockchainsProvider _blockchainsProvider;
        private readonly CloudTable _cashoutBatchesTable;
        private readonly CloudTable _operationExecutionsTable;
        private IReadOnlyDictionary<Guid, OperationExecutionEntity> _operationExecutions;
        

        public BilCashoutsBatchWithdrawalsHistoryProvider(
            ILogFactory logFactory,
            BlockchainsProvider blockchainsProvider,
            AzureStorageSettings azureStorageSettings)
        {
            _log = logFactory.CreateLog(this);
            _blockchainsProvider = blockchainsProvider;

            var cashoutProcessorAzureAccount = CloudStorageAccount.Parse(azureStorageSettings.CashoutProcessorConnString);
            var cashoutProcessorAzureClient = cashoutProcessorAzureAccount.CreateCloudTableClient();

            _cashoutBatchesTable = cashoutProcessorAzureClient.GetTableReference("CashoutsBatch");

            var operationsExecutorAzureAccount = CloudStorageAccount.Parse(azureStorageSettings.OperationsExecutorConnString);
            var operationsExecutorAzureClient = operationsExecutorAzureAccount.CreateCloudTableClient();

            _operationExecutionsTable = operationsExecutorAzureClient.GetTableReference("OperationExecutions");
        }

        public async Task<PaginatedList<Transaction>> GetHistoryAsync(string continuation)
        {
            var operationExecutions = await GetOperationExecutionsAsync();
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

                    if (!operationExecutions.TryGetValue(cashoutsBatch.BatchId, out var operationExecution))
                    {
                        _log.Warning($"Operation execution for cashouts batch {cashoutsBatch.BatchId} not found, skipping");

                        return Enumerable.Empty<Transaction>();
                    }

                    if (operationExecution.Result != OperationExecutionResult.Completed &&
                        operationExecution.Result != OperationExecutionResult.Success)
                    {
                        return Enumerable.Empty<Transaction>();
                    }

                    if (string.IsNullOrWhiteSpace(operationExecution.TransactionHash))
                    {
                        _log.Warning($"Transaction hash for cashouts batch {cashoutsBatch.BatchId} is empty, skipping");

                        return Enumerable.Empty<Transaction>();
                    }

                    var cashouts = JsonConvert.DeserializeObject<BatchedCashout[]>(cashoutsBatch.Cashouts);

                    return cashouts.Select(cashout => new Transaction
                    (
                        blockchain.CryptoCurrency,
                        operationExecution.TransactionHash,
                        cashout.ClientId,
                        cashout.DestinationAddress,
                        TransactionType.Withdrawal
                    ));
                })
                .ToArray();

            return PaginatedList.From(response.ContinuationToken, transactions);
        }

        private async Task<IReadOnlyDictionary<Guid, OperationExecutionEntity>> GetOperationExecutionsAsync()
        {
            if (_operationExecutions != null)
            {
                return _operationExecutions;
            }

            _log.Info("Loading operation executions...");

            var query = new TableQuery<OperationExecutionEntity>
            {
                TakeCount = 1000
            };
            TableContinuationToken continuationToken = null;
            var result = new List<OperationExecutionEntity>(131072);

            do
            {
                var response = await _operationExecutionsTable.ExecuteQuerySegmentedAsync(query, continuationToken);

                continuationToken = response.ContinuationToken;

                result.AddRange(response.Results);

                _log.Info($"{result.Count / 1000 * 1000} operation executions loaded so far");

            } while (continuationToken != null);

            _operationExecutions = result.ToDictionary(x => x.OperationId);

            _log.Info($"Operation executions loading done. {_operationExecutions.Count} operation executions loaded");

            return _operationExecutions;
        }
    }
}
