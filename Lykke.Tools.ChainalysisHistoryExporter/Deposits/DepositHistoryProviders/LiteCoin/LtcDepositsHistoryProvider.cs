using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Flurl.Http;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Lykke.Tools.ChainalysisHistoryExporter.InsightApi;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Lykke.Tools.ChainalysisHistoryExporter.Deposits.DepositHistoryProviders.LiteCoin
{
    public class LtcDepositsHistoryProvider : IDepositsHistoryProvider
    {
        private class ContinuationToken
        {
            public int Page { get; set; }
        }

        private readonly ILogger<LtcDepositsHistoryProvider> _logger;
        private readonly InsightApiClient _insightApi;
        private readonly Blockchain _liteCoin;

        public LtcDepositsHistoryProvider(
            ILogger<LtcDepositsHistoryProvider> logger,
            IOptions<LtcSettings> settings,
            BlockchainsProvider blockchainsProvider)
        {
            _logger = logger;
            _insightApi = new InsightApiClient(settings.Value.InsightApiUrl);
            _liteCoin = blockchainsProvider.GetLiteCoin();
        }

        public bool CanProvideHistoryFor(DepositWallet depositWallet)
        {
            return depositWallet.CryptoCurrency == _liteCoin.CryptoCurrency;
        }

        public async Task<PaginatedList<Transaction>> GetHistoryAsync(DepositWallet depositWallet, string continuation)
        {
            if (!CanProvideHistoryFor(depositWallet))
            {
                return PaginatedList.From(Array.Empty<Transaction>());
            }

            var continuationToken = continuation != null
                ? JsonConvert.DeserializeObject<ContinuationToken>(continuation)
                : new ContinuationToken {Page = 0};

            InsightApiTransactionsResponse response = null;

            try
            {
                response = await _insightApi.GetAddressTransactions(depositWallet.Address, continuationToken.Page);
            }
            catch (FlurlHttpException ex) when (ex.Call.HttpStatus == HttpStatusCode.BadRequest)
            {
                var responseMessage = await ex.GetResponseStringAsync();

                if (responseMessage == "Invalid address. Code:-5")
                {
                    _logger.LogWarning($"Insight API treated address [{depositWallet.Address}] as invalid. Skipping");
                }
                else
                {
                    throw;
                }
            }

            if (response == null)
            {
                return PaginatedList.From(Array.Empty<Transaction>());
            }

            var depositOperations = response.Transactions.Where(tx => IsDeposit(tx, depositWallet));
            var depositTransactions = Map(depositOperations, depositWallet);

            var nextPage = continuationToken.Page + 1;

            var resultContinuation = nextPage < response.PagesTotal
                ? new ContinuationToken {Page = nextPage}
                : null;

            return PaginatedList.From(resultContinuation, depositTransactions);
        }

        private IReadOnlyCollection<Transaction> Map(IEnumerable<InsightApiTransaction> insightApiTransactions, DepositWallet depositWallet)
        {
            return insightApiTransactions
                .Select(tx => new Transaction
                (
                    _liteCoin.CryptoCurrency,
                    tx.Id,
                    depositWallet.UserId,
                    depositWallet.Address,
                    TransactionType.Deposit
                ))
                .ToArray();
        }

        private static bool IsDeposit(InsightApiTransaction tx, DepositWallet depositWallet)
        {
            return tx.Inputs.All(input => input.Address != depositWallet.Address);
        }
    }
}
