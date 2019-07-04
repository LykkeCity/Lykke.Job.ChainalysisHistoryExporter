using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Common.Log;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Configuration;
using Lykke.Job.ChainalysisHistoryExporter.Deposits;
using Microsoft.Extensions.Options;

namespace Lykke.Job.ChainalysisHistoryExporter.Reporting
{
    public class DepositWalletsReport
    {
        private readonly IOptions<ReportSettings> _reportSettings;
        private readonly ILog _log;

        public DepositWalletsReport(
            ILogFactory logFactory,
            IOptions<ReportSettings> reportSettings)
        {
            _log = logFactory.CreateLog(this);
            _reportSettings = reportSettings;
        }

        public async Task SaveAsync(ISet<DepositWallet> depositWallets)
        {
            var filePath = _reportSettings.Value.DepositWalletsFilePath;

            _log.Info($"Saving deposit wallets. report to {filePath}..");

            var stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            
            using(var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                foreach (var wallet in depositWallets)
                {
                    await writer.WriteLineAsync($"{wallet.UserId},{wallet.CryptoCurrency},{wallet.Address}");
                }
            }

            _log.Info($"Deposit wallets saving done. {depositWallets.Count} deposit wallets saved");
        }
    }
}
