using System;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;

namespace Lykke.Tools.ChainalysisHistoryExporter.Deposits.DepositHistoryProviders
{
    internal class DepositsHistoryProvider : IDepositsHistoryProvider
    {
        public Task<PaginatedList<Transaction>> GetHistoryAsync(DepositWallet depositWallet, string continuation)
        {
            return Task.FromResult(PaginatedList.From(default(Transaction), Array.Empty<Transaction>()));
        }
    }
}
