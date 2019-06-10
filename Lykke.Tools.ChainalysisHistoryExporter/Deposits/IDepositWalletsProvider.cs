using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;

namespace Lykke.Tools.ChainalysisHistoryExporter.Deposits
{
    public interface IDepositWalletsProvider
    {
        Task<PaginatedList<DepositWallet>> GetWalletsAsync(string continuation);
    }
}
