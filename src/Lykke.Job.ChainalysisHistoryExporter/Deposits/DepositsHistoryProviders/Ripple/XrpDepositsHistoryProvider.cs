using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Flurl.Http;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Lykke.Job.ChainalysisHistoryExporter.Settings;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits.DepositsHistoryProviders.Ripple
{
    public class XrpDepositsHistoryProvider : IDepositsHistoryProvider
    {
        private readonly Blockchain _ripple;
        private readonly XrpSettings _settings;

        public XrpDepositsHistoryProvider(
            IBlockchainsProvider blockchainsProvider,
            XrpSettings settings)
        {
            _ripple = blockchainsProvider.GetRipple();
            _settings = settings;
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

            if (!long.TryParse(continuation, out var ledgerIndexMin))
            {
                ledgerIndexMin = -1;
            }

            var addressParts = depositWallet.Address.Split('+', StringSplitOptions.RemoveEmptyEntries);
            var address = addressParts[0];
            var tag = addressParts.Length > 1
                ? addressParts[1]
                : null;

            var transactions = new List<Transaction>();
            object pagingMarker = null;

            do
            {
                var response = await _settings.RpcUrl
                    .PostJsonAsync(new RippleAccountTransactionsRequest(address, ledgerIndexMin, pagingMarker))
                    .ReceiveJson<RippleAccountTransactionsResponse>();

                if (!string.IsNullOrEmpty(response.Result.Error))
                {
                    throw new InvalidOperationException($"XRP request error: {response.Result.Error}");
                }

                transactions.AddRange
                (
                    response.Result.Transactions.Where(tx => IsDeposit(tx, address, tag)).Select(tx => Map(tx, depositWallet))
                );

                pagingMarker = response.Result.Marker;
                continuation = response.Result.LedgerIndexMax.ToString("D");

            } while (pagingMarker != null);

            return PaginatedList.From
            (
                continuation,
                transactions
            );
        }

        private bool IsDeposit(RippleTransaction tx, string address, string tag)
        {
            return
                tx.Validated &&
                tx.Meta.TransactionResult == "tesSUCCESS" &&
                tx.Tx.TransactionType == "Payment" &&
                tx.Tx.Destination == address &&
                (tag == null || tx.Tx.DestinationTag?.ToString("D") == tag);
        }

        private Transaction Map(RippleTransaction tx, DepositWallet wallet)
        {
            return new Transaction(wallet.CryptoCurrency, tx.Tx.Hash, wallet.UserId, wallet.Address, TransactionType.Deposit);
        }
    }
}
