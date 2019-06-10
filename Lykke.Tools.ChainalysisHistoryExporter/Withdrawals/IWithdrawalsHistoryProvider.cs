using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;
using Lykke.Tools.ChainalysisHistoryExporter.Reporting;

namespace Lykke.Tools.ChainalysisHistoryExporter.Withdrawals
{
    public interface IWithdrawalsHistoryProvider
    {
        Task<PaginatedList<Transaction>> GetHistoryAsync(string continuation);
    }
}
