using System.Threading.Tasks;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;

namespace Lykke.Job.ChainalysisHistoryExporter.Withdrawals
{
    public interface IWithdrawalsHistoryProvider
    {
        Task<PaginatedList<Transaction>> GetHistoryAsync(string continuation);
    }
}
