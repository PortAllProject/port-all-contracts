using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Xml;
using AElf.Client.Bridge;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Client.MerkleTree;
using AElf.Client.Oracle;
using AElf.Client.Report;
using AElf.EventHandler.Workers;
using AElf.Nethereum.Bridge;
using AElf.Nethereum.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.RabbitMQ;
using Volo.Abp.Threading;

namespace AElf.EventHandler;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpEventBusRabbitMqModule),
    typeof(AElfClientModule),
    typeof(AElfClientOracleModule),
    typeof(AElfClientReportModule),
    typeof(AElfClientBridgeModule),
    typeof(AElfClientMerkleTreeModule),
    typeof(AElfNethereumBridgeModule),
    typeof(AbpCachingStackExchangeRedisModule),
    typeof(AbpBackgroundWorkersModule)
)]
public class EventHandlerAppModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        // Just for logging.
        Configure<MessageQueueOptions>(options => { configuration.GetSection("MessageQueue").Bind(options); });

        Configure<AbpRabbitMqEventBusOptions>(options =>
        {
            var messageQueueConfig = configuration.GetSection("MessageQueue");
            options.ClientName = messageQueueConfig.GetSection("ClientName").Value;
            options.ExchangeName = messageQueueConfig.GetSection("ExchangeName").Value;
        });

        Configure<AbpRabbitMqOptions>(options =>
        {
            var messageQueueConfig = configuration.GetSection("MessageQueue");
            var hostName = messageQueueConfig.GetSection("HostName").Value;

            options.Connections.Default.HostName = hostName;
            options.Connections.Default.Port = int.Parse(messageQueueConfig.GetSection("Port").Value);
            options.Connections.Default.UserName = messageQueueConfig.GetSection("UserName").Value;
            options.Connections.Default.Password = messageQueueConfig.GetSection("Password").Value;
            options.Connections.Default.Ssl = new SslOption
            {
                Enabled = true,
                ServerName = hostName,
                Version = SslProtocols.Tls12,
                AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch |
                                         SslPolicyErrors.RemoteCertificateChainErrors
            };
            options.Connections.Default.VirtualHost = "/";
            options.Connections.Default.Uri = new Uri(messageQueueConfig.GetSection("Uri").Value);
        });

        Configure<OracleOptions>(configuration.GetSection("Oracle"));
        Configure<BridgeOptions>(configuration.GetSection("Bridge"));
        Configure<BlockConfirmationOptions>(configuration.GetSection("BlockConfirmation"));
        Configure<ChainIdMappingOptions>(configuration.GetSection("ChainIdMapping"));
        Configure<FaultHandlingOptions>(configuration.GetSection("FaultHandling"));
        
        context.Services.AddHostedService<EventHandlerAppHostedService>();
        context.Services.AddTransient(typeof(ILogEventProcessor<>), typeof(LogEventProcessorBase<>));
        context.Services.AddSingleton<ITransmitTransactionProvider, TransmitTransactionProvider>();
        context.Services.AddSingleton<ISignatureRecoverableInfoProvider, SignatureRecoverableInfoProvider>();
    }
    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        context.AddBackgroundWorkerAsync<TransmitTransactionWorker>();
        context.AddBackgroundWorkerAsync<ReceiptSyncWorker>();

        var faultHandlingOptions = context.ServiceProvider.GetRequiredService<IOptionsSnapshot<FaultHandlingOptions>>();
        if (faultHandlingOptions.Value.IsReSendFailedJob)
        {
            var service = context.ServiceProvider.GetRequiredService<ITransmitTransactionProvider>();
            AsyncHelper.RunSync(async()=> await service.ReSendFailedJobAsync(faultHandlingOptions.Value.ReSendFailedJobChainId));
        }
    }
}