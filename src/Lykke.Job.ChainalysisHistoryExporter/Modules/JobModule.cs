using Autofac;
using Lykke.Job.ChainalysisHistoryExporter.Services;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Lykke.Sdk;
using Lykke.Sdk.Health;
using Lykke.SettingsReader;

namespace Lykke.Job.ChainalysisHistoryExporter.Modules
{
    public class JobModule : Module
    {
        private AppSettings _settings;

        public JobModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings.CurrentValue;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>()
                .SingleInstance();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>()
                .AutoActivate()
                .SingleInstance();
        }
    }
}
