using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Microsoft.Extensions.Caching.Memory;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.Ripple
{
    public class XrpDepositsHistoryProvider : IDepositsHistoryProvider
    {
        private readonly Blockchain _ripple;
        private readonly IMemoryCache _memoryCache;
        private readonly XrpSettings _settings;

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

            if (!_memoryCache.TryGetValue<List<RippleTransaction>>(address, out var txs))
            {
                txs = await GetRippleTransactions(address);

                _memoryCache.Set(address, txs, _settings.CacheExpirationPeriod);
            }

            return PaginatedList.From(
                GetDeposits(txs, depositWallet.UserId, address, tag)
            );
        }

        private async Task<List<RippleTransaction>> GetRippleTransactions(string address)
        {
            var txs = new List<RippleTransaction>();
            object pagingMarker = null;

            do
            {
                var request = new FlurlRequest(_settings.RpcUrl);

                if (!string.IsNullOrEmpty(_settings.RpcUsername))
                {
                    request = request.WithBasicAuth(_settings.RpcUsername, _settings.RpcPassword);
                }
                    
                var response = await request
                    .PostJsonAsync(new RippleAccountTransactionsRequest(address, marker: pagingMarker))
                    .ReceiveJson<RippleAccountTransactionsResponse>();

                if (!string.IsNullOrEmpty(response.Result.Error))
                {
                    throw new InvalidOperationException($"XRP request error: {response.Result.Error}");
                }

                txs.AddRange(response.Result.Transactions);

                pagingMarker = response.Result.Marker;

            } while (pagingMarker != null);

            return txs;
        }

        private List<Transaction> GetDeposits(IEnumerable<RippleTransaction> txs, Guid userId, string address, string tag)
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
                // but consumer interested in real blockchain address only,
                // so instead of deposit wallet address in form "{address}+{tag}"
                // return just Ripple address without tag
                .Select(tx => new Transaction(_ripple.CryptoCurrency, tx.Tx.Hash, userId, address, TransactionType.Deposit))
                .ToList();
        }
    }
}
