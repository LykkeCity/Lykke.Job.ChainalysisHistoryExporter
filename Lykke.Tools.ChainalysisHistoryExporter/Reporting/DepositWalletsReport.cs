using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Lykke.Tools.ChainalysisHistoryExporter.Configuration;
using Lykke.Tools.ChainalysisHistoryExporter.Deposits;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lykke.Tools.ChainalysisHistoryExporter.Reporting
{
    public class DepositWalletsReport
    {
        private readonly ILogger<DepositWalletsReport> _logger;
        private readonly IOptions<ReportSettings> _reportSettings;

        public DepositWalletsReport(
            ILogger<DepositWalletsReport> logger,
            IOptions<ReportSettings> reportSettings)
        {
            _logger = logger;
            _reportSettings = reportSettings;
        }

        public async Task SaveAsync(ISet<DepositWallet> depositWallets)
        {
            var filePath = _reportSettings.Value.DepositWalletsFilePath;

            _logger.LogInformation($"Saving deposit wallets. report to {filePath}..");

            var stream = File.Open(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            
            using(var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                foreach (var wallet in depositWallets)
                {
                    await writer.WriteLineAsync($"{wallet.UserId},{wallet.CryptoCurrency},{wallet.Address}");
                }
            }

            _logger.LogInformation($"Deposit wallets saving done. {depositWallets.Count} deposit wallets saved");
        }
    }
}
