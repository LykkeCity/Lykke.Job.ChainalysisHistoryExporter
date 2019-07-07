using System.Threading.Tasks;
using Common.Log;
using Hangfire;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Jobs;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Lykke.Sdk;

namespace Lykke.Job.ChainalysisHistoryExporter.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly ScheduleSettings _scheduleSettings;
        private readonly ILog _log;

        public StartupManager(
            ILogFactory logFactory,
            ScheduleSettings scheduleSettings
            )
        {
            _scheduleSettings = scheduleSettings;
            _log = logFactory.CreateLog(this);
        }

        public Task StartAsync()
        {
            _log.Info($"Registering {nameof(ExportHistoryJob)} as recurring job '{ExportHistoryJob.Id}' with CRON '{_scheduleSettings.ExportHistoryCron}'...");

            RecurringJob.AddOrUpdate<ExportHistoryJob>
            (
                recurringJobId: ExportHistoryJob.Id,
                methodCall: job => job.ExecuteAsync(),
                cronExpression: _scheduleSettings.ExportHistoryCron
            );

            return Task.CompletedTask;
        }
    }
}
