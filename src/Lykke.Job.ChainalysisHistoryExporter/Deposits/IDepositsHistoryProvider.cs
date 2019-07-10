using System.Threading.Tasks;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits
{
    public interface IDepositsHistoryProvider
    {
        bool CanProvideHistoryFor(DepositWallet depositWallet);
        Task<PaginatedList<Transaction>> GetHistoryAsync(DepositWallet depositWallet, string continuation);
    }
}
