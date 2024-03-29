﻿using System;
using System.Collections.Generic;
using Autofac;
using Lykke.Job.ChainalysisHistoryExporter.AddressNormalization;
using Lykke.Job.ChainalysisHistoryExporter.Assets;
using Lykke.Job.ChainalysisHistoryExporter.Common;
using Lykke.Job.ChainalysisHistoryExporter.Deposits;
using Lykke.Job.ChainalysisHistoryExporter.Reporting;
using Lykke.Job.ChainalysisHistoryExporter.Settings;
using Lykke.Job.ChainalysisHistoryExporter.Withdrawals;
using Lykke.Logs;
using Lykke.Service.EmailSender;
using Lykke.SettingsReader;

namespace Lykke.Job.ChainalysisHistoryExporter.Modules
{
    public class ExporterModule : Module
    {
        private readonly AppSettings _settings;

        public ExporterModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings.CurrentValue;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_settings.Email);
            builder.RegisterInstance(_settings.AzureStorage);
            builder.RegisterInstance(_settings.MongoStorage);
            builder.RegisterInstance(_settings.Slack);
            builder.RegisterInstance(_settings.Btc);
            builder.RegisterInstance(_settings.Eth);
            builder.RegisterInstance(_settings.Xrp);

            builder.RegisterType<Exporter>().AsSelf();

            builder.RegisterType<TransactionsReportBuilder>()
                .AsSelf();

            builder.RegisterType<TransactionsReportReader>().AsSelf();
            builder.RegisterType<TransactionsReportWriter>().AsSelf();
            builder.RegisterType<TransactionsSnapshotRepository>().AsSelf();

            RegisterImplementations<ITransactionsIncrementPublisher>(builder, _settings.Report.Publishers);

            builder.RegisterType<AssetsClient>()
                .WithParameter(TypedParameter.From(_settings.Assets))
                .AsSelf()
                .SingleInstance();

            builder.RegisterEmailSenderService(_settings.Email.EmailSenderServiceUrl, EmptyLogFactory.Instance.CreateLog(this));

            builder.RegisterType<BlockchainsProvider>().As<IBlockchainsProvider>().SingleInstance();
            builder.RegisterType<AddressNormalizer>().AsSelf().SingleInstance();
            builder.RegisterType<WithdrawalsExporter>().AsSelf();
            builder.RegisterType<DepositsExporter>().AsSelf();

            builder.RegisterType<GeneralAddressNormalizer>().As<IAddressNormalizer>();
            builder.RegisterType<BtcAddressNormalizer>().As<IAddressNormalizer>();
            builder.RegisterType<EthAddressNormalizer>().As<IAddressNormalizer>();
            builder.RegisterType<XrpAddressNormalizer>().As<IAddressNormalizer>();

            RegisterImplementations<IWithdrawalsHistoryProvider>(builder, _settings.WithdrawalsHistoryProviders.Providers);
            RegisterImplementations<IDepositWalletsProvider>(builder, _settings.DepositWalletsProviders.Providers);
            RegisterImplementations<IDepositsHistoryProvider>(builder, _settings.DepositsHistoryProviders.Providers);
        }

        private static void RegisterImplementations<TService>(ContainerBuilder builder, IEnumerable<string> implementationNames)
        {
            var serviceType = typeof(TService);
            var serviceAssembly = serviceType.Assembly;
            var serviceNamespace = serviceType.Namespace;
            var implementationsSubNamespace = GetImplementationsSubNamespace(serviceType);

            foreach (var implementationName in implementationNames)
            {
                var implementationFullName = $"{serviceNamespace}.{implementationsSubNamespace}.{implementationName}";
                var implementationType = serviceAssembly.GetType(implementationFullName, throwOnError: true);

                builder.RegisterType(implementationType).As<TService>();
            }
        }

        private static string GetImplementationsSubNamespace(Type serviceType)
        {
            var serviceTypeName = serviceType.Name;

            if (serviceTypeName.EndsWith('y'))
            {
                return $"{serviceTypeName.Substring(1, serviceTypeName.Length - 2)}ies";
            }

            if (serviceTypeName.EndsWith('s'))
            {
                return $"{serviceTypeName.Substring(1)}es";
            }

            return $"{serviceType.Name.Substring(1)}s";
        }
    }
}
