using System;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositWalletsProviders
{
    public class BilMongoDepositWalletsProvider : IDepositWalletsProvider
    {
        #region Entities

        // ReSharper disable StringLiteralTypo
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        // ReSharper disable ClassNeverInstantiated.Local
        // ReSharper disable UnusedMember.Local

        private enum CreatorType
        {
            LykkeWallet = 1,
            LykkePay = 2
        }

        
        private class WalletMongoEntity
        {
            [BsonId] 
            public ObjectId Id { get; set; }
            
            [BsonElement("clid")] 
            
            public Guid ClientId { get; set; }
            
            [BsonElement("btyp")] 
            public string BlockchainType { get; set; }

            [BsonElement("crtr")]
            public CreatorType CreatorType { get; set; }
            
            [BsonElement("addr")] 
            public string Address { get; set; }

            [BsonElement("ins")] 
            public DateTime Inserted { get; set; }

            [BsonElement("upd")] 
            public DateTime Updated { get; set; }

            [BsonElement("vers")]
            public int Version { get; set; }
        }

        // ReSharper restore StringLiteralTypo
        // ReSharper restore UnusedAutoPropertyAccessor.Local
        // ReSharper restore ClassNeverInstantiated.Local
        // ReSharper restore UnusedMember.Local

        #endregion

        private class MongoContinuationToken
        {
            public int Skip { get; set; }
        }

        private readonly IBlockchainsProvider _blockchainsProvider;
        private readonly IMongoCollection<WalletMongoEntity> _collection;

        public BilMongoDepositWalletsProvider(
            IBlockchainsProvider blockchainsProvider,
            MongoStorageSettings settings)
        {
            _blockchainsProvider = blockchainsProvider;

            var client = new MongoClient(settings.BlockchainWalletsConnString);
            var db = client.GetDatabase(settings.BlockchainWalletsDbName);

            _collection = db.GetCollection<WalletMongoEntity>("blockchain-wallets");
        }

        public async Task<PaginatedList<DepositWallet>> GetWalletsAsync(string continuation)
        {
            if (_collection == null)
            {
                throw new InvalidOperationException("MongoStorage not configured");
            }

            var continuationToken = continuation != null
                ? JsonConvert.DeserializeObject<MongoContinuationToken>(continuation)
                : new MongoContinuationToken {Skip = 0};

            var entities = await _collection.AsQueryable()
                .Skip(continuationToken.Skip)
                .Take(1000)
                .ToListAsync();
            var wallets = entities
                .Select(wallet =>
                {
                    var blockchain = _blockchainsProvider.GetByBilIdOrDefault(wallet.BlockchainType);

                    if (blockchain == null)
                    {
                        return null;
                    }

                    return new DepositWallet(wallet.ClientId, wallet.Address, blockchain.CryptoCurrency);
                })
                .Where(wallet => wallet != null)
                .ToArray();

            var resultContinuationToken = entities.Count < 1000
                ? null
                : new MongoContinuationToken
                {
                    Skip = continuationToken.Skip + entities.Count
                };

            return PaginatedList.From(resultContinuationToken, wallets);
        }
    }
}
