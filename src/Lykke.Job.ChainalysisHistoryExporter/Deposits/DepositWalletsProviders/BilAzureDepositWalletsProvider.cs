﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositWalletsProviders
{
    public class BilAzureDepositWalletsProvider : IDepositWalletsProvider
    {
        #region Entities

        // ReSharper disable UnusedAutoPropertyAccessor.Local

        private class BlockchainWalletEntity : TableEntity
        {
            public string Address { get; set; }
            
            public string IntegrationLayerId { get; set; }

            public Guid ClientId { get; set; }
        }

        // ReSharper restore UnusedAutoPropertyAccessor.Local

        #endregion

        private readonly IBlockchainsProvider _blockchainsProvider;
        private readonly CloudTable _table;

        public BilAzureDepositWalletsProvider(
            AzureStorageSettings azureStorageSettings,
            IBlockchainsProvider blockchainsProvider)
        {
            _blockchainsProvider = blockchainsProvider;

            var azureAccount = CloudStorageAccount.Parse(azureStorageSettings.BlockchainWalletsConnString);
            var azureClient = azureAccount.CreateCloudTableClient();

            _table = azureClient.GetTableReference(azureStorageSettings.BlockchainWalletsTable);
        }

        public async Task<PaginatedList<DepositWallet>> GetWalletsAsync(string continuation)
        {
            var continuationToken = continuation != null
                ? JsonConvert.DeserializeObject<TableContinuationToken>(continuation)
                : null;
            var query = new TableQuery<BlockchainWalletEntity>
            {
                TakeCount = 1000
            };
            var response = await _table.ExecuteQuerySegmentedAsync(query, continuationToken);

            var transactions = response.Results
                .Select(wallet =>
                {
                    var blockchain = _blockchainsProvider.GetByBilIdOrDefault(wallet.IntegrationLayerId);

                    if (blockchain == null)
                    {
                        return null;
                    }

                    return new DepositWallet
                    (
                        wallet.ClientId,
                        wallet.Address,
                        blockchain.CryptoCurrency
                    );
                })
                .Where(x => x != null)
                .ToArray();

            return PaginatedList.From(response.ContinuationToken, transactions);
        }
    }
}
