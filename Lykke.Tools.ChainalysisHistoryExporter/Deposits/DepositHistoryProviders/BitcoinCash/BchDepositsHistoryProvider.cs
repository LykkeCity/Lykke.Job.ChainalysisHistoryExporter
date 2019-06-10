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
using NBitcoin;
using NBitcoin.Altcoins;
using Newtonsoft.Json;
using Transaction = Lykke.Tools.ChainalysisHistoryExporter.Reporting.Transaction;

namespace Lykke.Tools.ChainalysisHistoryExporter.Deposits.DepositHistoryProviders.BitcoinCash
{
    public class BchDepositsHistoryProvider : IDepositsHistoryProvider
    {
        private class ContinuationToken
        {
            public int Page { get; set; }
        }

        private readonly ILogger<BchDepositsHistoryProvider> _logger;
        private readonly InsightApiClient _insightApi;
        private readonly Blockchain _bitcoinCash;
        private readonly Network _network;
        private readonly Network _bchNetwork;

        public BchDepositsHistoryProvider(
            ILogger<BchDepositsHistoryProvider> logger,
            IOptions<BchSettings> settings,
            BlockchainsProvider blockchainsProvider)
        {
            _logger = logger;
            _insightApi = new InsightApiClient(settings.Value.InsightApiUrl);
            _network = Network.GetNetwork(settings.Value.Network);
            _bchNetwork = _network == Network.Main ? BCash.Instance.Mainnet : BCash.Instance.Regtest;
            _bitcoinCash = blockchainsProvider.GetBitcoinCash();
        }

        public bool CanProvideHistoryFor(DepositWallet depositWallet)
        {
            return depositWallet.CryptoCurrency == _bitcoinCash.CryptoCurrency;
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

            var address = NormalizeAddress(depositWallet.Address);

            try
            {
                response = await _insightApi.GetAddressTransactions
                (
                    address,
                    continuationToken.Page
                );
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

            var depositOperations = response.Transactions.Where(tx => IsDeposit(tx, address));
            var depositTransactions = Map(depositOperations, depositWallet);

            var nextPage = continuationToken.Page + 1;

            var resultContinuation = nextPage < response.PagesTotal
                ? new ContinuationToken {Page = nextPage}
                : null;

            return PaginatedList.From(resultContinuation, depositTransactions);
        }

        private string NormalizeAddress(string address)
        {
            // ReSharper disable CommentTypo
            // eg: moc231tgxApbRSwLNrc9ZbSVDktTRo3acK
            var legacyAddress = GetBitcoinAddress(address, _network);
            if (legacyAddress != null)
            {
                return legacyAddress.ScriptPubKey.GetDestinationAddress(_bchNetwork).ToString();
            }

            // eg: bitcoincash:qpm2qsznhks23z7629mms6s4cwef74vcwvy22gdx6a
            var canonicalAddress = GetBitcoinAddress(address, _bchNetwork);
            if (canonicalAddress != null)
            {
                return canonicalAddress.ToString();
            }
            
            // eg: qpm2qsznhks23z7629mms6s4cwef74vcwvy22gdx6a
            // ReSharper restore CommentTypo
            var addressWithoutPrefix = GetBitcoinAddress($"{GetAddressPrefix(_bchNetwork)}:{address.Trim()}", _bchNetwork);

            return addressWithoutPrefix.ToString();
        }

        private static BitcoinAddress GetBitcoinAddress(string address, Network network)
        {
            try
            {
                return BitcoinAddress.Create(address, network);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string GetAddressPrefix(Network bchNetwork)
        {
            if (bchNetwork == BCash.Instance.Mainnet)
            {
                return "bitcoincash";
            }
            if (bchNetwork == BCash.Instance.Regtest)
            {
                return "bchreg";
            }
            if (bchNetwork == BCash.Instance.Testnet)
            {
                return "bchtest";
            }

            throw new ArgumentException("Unknown Bitcoin Cash network", nameof(bchNetwork));
        }

        private static IReadOnlyCollection<Transaction> Map(IEnumerable<InsightApiTransaction> insightApiTransactions, DepositWallet depositWallet)
        {
            return insightApiTransactions
                .Select(tx => new Transaction
                (
                    depositWallet.CryptoCurrency,
                    tx.Id,
                    depositWallet.UserId,
                    depositWallet.Address,
                    TransactionType.Deposit
                ))
                .ToArray();
        }

        private bool IsDeposit(InsightApiTransaction tx, string address)
        {
            return tx.Inputs.All(input => NormalizeAddress(input.Address) != address);
        }
    }
}
