using System.Threading.Tasks;
using Lykke.Job.ChainalysisHistoryExporter.Common;

namespace Lykke.Job.ChainalysisHistoryExporter.Deposits
{
    public interface IDepositWalletsProvider
    {
        Task<PaginatedList<DepositWallet>> GetWalletsAsync(string continuation);
    }
}
