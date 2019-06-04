using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;

namespace Lykke.Tools.ChainalysisHistoryExporter.Deposits
{
    internal interface IDepositsHistoryProvider
    {
        Task<PaginatedList<Transaction>> GetHistoryAsync(DepositWallet depositWallet, string continuation);
    }
}
