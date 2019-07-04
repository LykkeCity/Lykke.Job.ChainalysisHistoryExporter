using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common.Log;
using Flurl.Http;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.AddressNormalization;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Configuration;
using Lykke.Job.ChainalysisHistoryExporter.InsightApi;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Transaction = Lykke.Job.ChainalysisHistoryExporter.Reporting.Transaction;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositHistoryProviders.LiteCoin
{
    public class LtcDepositsHistoryProvider : IDepositsHistoryProvider
    {
        private class ContinuationToken
        {
            public int Page { get; set; }
        }

        private readonly ILog _log;
        private readonly AddressNormalizer _addressNormalizer;
        private readonly InsightApiClient _insightApi;
        private readonly Blockchain _liteCoin;
        
        public LtcDepositsHistoryProvider(
            ILogFactory logFactory,
            IOptions<LtcSettings> settings,
            BlockchainsProvider blockchainsProvider,
            AddressNormalizer addressNormalizer)
        {
            _log = logFactory.CreateLog(this);
            _addressNormalizer = addressNormalizer;
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
                    _log.Warning
                    (
                        "Insight API treated address as invalid. Skipping",
                        context: new
                        {
                            Address = depositWallet.Address
                        }
                    );
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

            var depositOperations = response.Transactions.Where(tx => IsDeposit(tx, depositWallet.Address));
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

        private bool IsDeposit(InsightApiTransaction tx, string address)
        {
            return tx.Inputs.All(input => !string.Equals(_addressNormalizer.NormalizeOrDefault(input.Address, _liteCoin.CryptoCurrency), address, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
