using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Flurl.Http;
using Flurl.Http.Configuration;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Newtonsoft.Json;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.Ripple
{
    public class XrpDepositsHistoryProvider : IDepositsHistoryProvider
    {
        private readonly Blockchain _ripple;
        private readonly IMemoryCache _memoryCache;
        private readonly XrpSettings _settings;
        private readonly ConcurrentDictionary<string, object> _mutex = new ConcurrentDictionary<string, object>();

        public XrpDepositsHistoryProvider(
            IBlockchainsProvider blockchainsProvider,
            XrpSettings settings)
        {
            _ripple = blockchainsProvider.GetRipple();
            _settings = settings;

            if (_settings.CacheExpirationPeriod == default(TimeSpan))
                _settings.CacheExpirationPeriod = TimeSpan.FromMinutes(5);

            _memoryCache = new MemoryCache(new MemoryCacheOptions());
        }

        public bool CanProvideHistoryFor(DepositWallet depositWallet)
        {
            return depositWallet.CryptoCurrency == _ripple.CryptoCurrency;
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

            var addressParts = depositWallet.Address.Split('+', StringSplitOptions.RemoveEmptyEntries);
            var address = addressParts[0];
            var tag = addressParts.Length > 1
                ? addressParts[1]
                : null;

            List<RippleTransaction> txs;

            if (!_memoryCache.TryGetValue<List<RippleTransaction>>(address, out txs))
            {
                lock (_mutex.GetOrAdd(address, new object()))
                {
                    if (!_memoryCache.TryGetValue<List<RippleTransaction>>(address, out txs))
                    {
                        txs = GetRippleTransactions(address).Result;

                        _memoryCache.Set(address, txs, _settings.CacheExpirationPeriod);
                    }
                }
            }
            
            return PaginatedList.From(
                GetDeposits(txs, depositWallet, address, tag)
            );
        }

        private async Task<List<RippleTransaction>> GetRippleTransactions(string address)
        {
            var txs = new List<RippleTransaction>();
            object pagingMarker = null;

            do
            {
                var request = new FlurlRequest(_settings.RpcUrl);

                request.Settings.JsonSerializer = new NewtonsoftJsonSerializer(new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });

                if (!string.IsNullOrEmpty(_settings.RpcUsername))
                {
                    request = request.WithBasicAuth(_settings.RpcUsername, _settings.RpcPassword);
                }

                var response = await request
                    .PostJsonAsync(new RippleAccountTransactionsRequest(address, marker: pagingMarker))
                    .ReceiveJson<RippleAccountTransactionsResponse>();

                if (!string.IsNullOrEmpty(response.Result.Error))
                {
                    throw new InvalidOperationException($"XRP request error: {response.Result.ErrorMessage ?? response.Result.Error}");
                }

                txs.AddRange(response.Result.Transactions);

                pagingMarker = response.Result.Marker;

            } while (pagingMarker != null);

            return txs;
        }

        private List<Transaction> GetDeposits(IEnumerable<RippleTransaction> txs, DepositWallet wallet, string address, string tag)
        {
            return txs
                // filter transaction by destination tag
                // to find deposits of specified user
                .Where(tx =>
                    tx.Validated &&
                    tx.Meta.TransactionResult == "tesSUCCESS" &&
                    tx.Tx.TransactionType == "Payment" &&
                    tx.Tx.Destination == address &&
                    (tag == null || tx.Tx.DestinationTag?.ToString("D") == tag))
                .Select(tx => new Transaction(wallet.CryptoCurrency, tx.Tx.Hash, wallet.UserId, wallet.Address, TransactionType.Deposit))
                .ToList();
        }
    }
}
