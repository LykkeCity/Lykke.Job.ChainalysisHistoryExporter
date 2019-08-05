using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.Log;
using Cronos;
using Hangfire;
using Lykke.Common.Log;
using Lykke.Job.ChainalysisHistoryExporter.Jobs;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Lykke.Sdk;
using NBitcoin.Altcoins;

namespace Lykke.Job.ChainalysisHistoryExporter.Services
{
    public class StartupManager : IStartupManager
    {
        private readonly ScheduleSettings _scheduleSettings;
        private readonly TransactionsSnapshotRepository _transactionsSnapshotRepository;
        private readonly ILog _log;

        public StartupManager(
            ILogFactory logFactory,
            ScheduleSettings scheduleSettings,
            TransactionsSnapshotRepository transactionsSnapshotRepository)
        {
            _scheduleSettings = scheduleSettings;
            _transactionsSnapshotRepository = transactionsSnapshotRepository;
            _log = logFactory.CreateLog(this);
        }

        public Task StartAsync()
        {
            _log.Info("Ensuring BitcoinCash is registered...");

            BCash.Instance.EnsureRegistered();

            _log.Info("Ensuring LiteCoin is registered...");

            Litecoin.Instance.EnsureRegistered();

            _log.Info($"Registering {nameof(ExportHistoryJob)} as recurring job '{ExportHistoryJob.Id}' with CRON '{_scheduleSettings.ExportHistoryCron}'...");

            RecurringJob.AddOrUpdate<ExportHistoryJob>
            (
                recurringJobId: ExportHistoryJob.Id,
                methodCall: job => job.ExecuteAsync(),
                cronExpression: _scheduleSettings.ExportHistoryCron
            );
            
            RunExportJobIfWasMissed();

            return Task.CompletedTask;
        }

        private void RunExportJobIfWasMissed()
        {
            var scheduleCron = CronExpression.Parse(_scheduleSettings.ExportHistoryCron);

            var lastOccurrence = _transactionsSnapshotRepository.GetLastModified()?.UtcDateTime;
            var now = DateTime.UtcNow;

            _log.Info($"Last report occurence is [{lastOccurrence:s}]");

            var missedOccurrences = GetMissedOccurrenceAsync(scheduleCron, lastOccurrence, now);

            _log.Info("Missed report occurrences", missedOccurrences);

            if (!lastOccurrence.HasValue || missedOccurrences.Any())
            {
                _log.Info("Triggering the job...");

                RecurringJob.Trigger(ExportHistoryJob.Id);
            }
        }

        private static IReadOnlyCollection<DateTime> GetMissedOccurrenceAsync(CronExpression scheduleCron, DateTime? lastOccurrence, DateTime now)
        {
            if (lastOccurrence.HasValue)
            {
                return scheduleCron.GetOccurrences
                    (
                        lastOccurrence.Value,
                        now,
                        false,
                        true
                    )
                    .ToArray();
            }

            return Array.Empty<DateTime>();
        }
    }
}
