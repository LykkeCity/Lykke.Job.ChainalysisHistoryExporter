using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Common;

namespace Lykke.Tools.ChainalysisHistoryExporter.Deposits
{
    internal interface IDepositWalletsProvider
    {
        Task<PaginatedList<DepositWallet>> GetWalletsAsync(string continuation);
    }
}
