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
    public class WalletCredentialsDepositWalletsProvider : IDepositWalletsProvider
    {
        #region Entities

        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedAutoPropertyAccessor.Local

        private class WalletCredentialsEntity : TableEntity
        {
            
            public string ClientId { get; set; }
            public string Address { get; set; }
            public string MultiSig { get; set; }
            public string ColoredMultiSig { get; set; }
            // ReSharper disable once IdentifierTypo
            public string BtcConvertionWalletAddress { get; set; }
            public string EthConversionWalletAddress { get; set; }
            public string EthAddress { get; set; }
            public string SolarCoinWalletAddress { get; set; }
            // ReSharper disable once IdentifierTypo
            public string ChronoBankContract { get; set; }
            public string QuantaContract { get; set; }
        }

        // ReSharper restore UnusedMember.Local
        // ReSharper restore UnusedAutoPropertyAccessor.Local

        #endregion

        private readonly IBlockchainsProvider _blockchainsProvider;
        private readonly CloudTable _table;

        public WalletCredentialsDepositWalletsProvider(
            IBlockchainsProvider blockchainsProvider,
            AzureStorageSettings azureStorageSettings)
        {
            _blockchainsProvider = blockchainsProvider;

            var azureAccount = CloudStorageAccount.Parse(azureStorageSettings.ClientPersonalInfoConnString);
            var azureClient = azureAccount.CreateCloudTableClient();

            _table = azureClient.GetTableReference("WalletCredentials");
        }

        public async Task<PaginatedList<DepositWallet>> GetWalletsAsync(string continuation)
        {
            var continuationToken = continuation != null
                ? JsonConvert.DeserializeObject<TableContinuationToken>(continuation)
                : null;
            var query = new TableQuery<WalletCredentialsEntity>
            {
                TakeCount = 1000
            };
            var response = await _table.ExecuteQuerySegmentedAsync(query, continuationToken);

            var transactions = response.Results
                .SelectMany(wallet =>
                {
                    var userId = Guid.Parse(wallet.ClientId);
                    var wallets = new List<DepositWallet>();
                    var bitcoin = _blockchainsProvider.GetBitcoin();

                    if (!string.IsNullOrEmpty(wallet.MultiSig))
                    {
                        wallets.Add(new DepositWallet
                        (
                            userId,
                            wallet.MultiSig,
                            bitcoin.CryptoCurrency
                        ));
                    }

                    if (!string.IsNullOrEmpty(wallet.ColoredMultiSig))
                    {
                        wallets.Add(new DepositWallet
                        (
                            userId,
                            wallet.ColoredMultiSig,
                            bitcoin.CryptoCurrency
                        ));
                    }

                    return wallets;
                })
                .ToArray();

            return PaginatedList.From(response.ContinuationToken, transactions);
        }
    }
}
