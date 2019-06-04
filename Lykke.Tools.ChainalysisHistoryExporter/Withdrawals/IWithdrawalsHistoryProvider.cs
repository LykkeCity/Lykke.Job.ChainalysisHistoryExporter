using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;

namespace Lykke.Tools.ChainalysisHistoryExporter.Withdrawals
{
    internal interface IWithdrawalsHistoryProvider
    {
        Task<PaginatedList<Transaction>> GetHistoryAsync(string continuation);
    }
}
