using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositWalletsProviders
{
    public class BcnCredentialsDepositWalletsProvider : IDepositWalletsProvider
    {
        #region Entities

        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Local

        private class BcnCredentialsRecordEntity : TableEntity
        {
            
            public string Address { get; set; }
            public string ClientId { get; set; }
            public string AssetAddress { get; set; }
            public string AssetId { get; set; }
        }

        // ReSharper restore UnusedMember.Local
        // ReSharper restore UnusedAutoPropertyAccessor.Local

        #endregion

        private readonly IBlockchainsProvider _blockchainsProvider;
        private readonly CloudTable _table;

        public BcnCredentialsDepositWalletsProvider(
            AzureStorageSettings azureStorageSettings,
            IBlockchainsProvider blockchainsProvider)
        {
            _blockchainsProvider = blockchainsProvider;

            var azureAccount = CloudStorageAccount.Parse(azureStorageSettings.ClientPersonalInfoConnString);
            var azureClient = azureAccount.CreateCloudTableClient();

            _table = azureClient.GetTableReference("BcnClientCredentials");
        }

        public async Task<PaginatedList<DepositWallet>> GetWalletsAsync(string continuation)
        {
            var continuationToken = continuation != null
                ? JsonConvert.DeserializeObject<TableContinuationToken>(continuation)
                : null;
            var query = new TableQuery<BcnCredentialsRecordEntity>
            {
                TakeCount = 1000
            };
            var response = await _table.ExecuteQuerySegmentedAsync(query, continuationToken);

            var transactions = response.Results
                .SelectMany(wallet =>
                {
                    var assetReference = wallet.AssetId.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    var blockchain = _blockchainsProvider.GuessBlockchainOrDefault(assetReference);

                    if (blockchain == null)
                    {
                        return Enumerable.Empty<DepositWallet>();
                    }

                    var clientId = Guid.Parse(wallet.ClientId);
                    var wallets = new List<DepositWallet>();

                    if (!string.IsNullOrWhiteSpace(wallet.Address))
                    {
                        wallets.Add(new DepositWallet
                        (
                            clientId,
                            wallet.Address,
                            blockchain.CryptoCurrency
                        ));
                    }

                    if (!string.IsNullOrWhiteSpace(wallet.AssetAddress))
                    {
                        wallets.Add(new DepositWallet
                        (
                            clientId,
                            wallet.AssetAddress,
                            blockchain.CryptoCurrency
                        ));
                    }

                    return wallets;
                })
                .ToArray();

            return PaginatedList.From(response.ContinuationToken, transactions);
        }
    }
}
