using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Lykke.Tools.ChainalysisHistoryExporter.Deposits.DepositWalletsProviders
{
    internal class BcnCredentialsDepositWalletsProvider : IDepositWalletsProvider
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

        private readonly BlockchainsProvider _blockchainsProvider;
        private readonly CloudTable _table;

        public BcnCredentialsDepositWalletsProvider(
            IOptions<AzureStorageSettings> azureStorageSettings,
            BlockchainsProvider blockchainsProvider)
        {
            _blockchainsProvider = blockchainsProvider;

            var azureAccount = CloudStorageAccount.Parse(azureStorageSettings.Value.ClientPersonalInfoConnString);
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
                .Select(wallet =>
                {
                    var assetId = wallet.AssetId.Split(new[] {' '}).FirstOrDefault();
                    var blockchain = _blockchainsProvider.GetByAssetIdOrDefault(assetId);

                    if (blockchain == null)
                    {
                        return null;
                    }

                    return new DepositWallet
                    (
                        Guid.Parse(wallet.ClientId),
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
