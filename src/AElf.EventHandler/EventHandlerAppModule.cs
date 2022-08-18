using System;
using System.Net.Security;
using System.Security.Authentication;
using AElf.Client.Bridge;
using AElf.Client.Core;
using AElf.Client.MerkleTree;
using AElf.Client.Oracle;
using AElf.Client.Report;
using AElf.Nethereum.Bridge;
using AElf.Nethereum.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Volo.Abp;
using Volo.Abp.Autofac;
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
    typeof(AbpCachingStackExchangeRedisModule)
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

        // Configure<ConfigOptions>(configuration.GetSection("Config"));
        // Configure<EthereumConfigOptions>(configuration.GetSection("Ethereum"));
        Configure<OracleOptions>(configuration.GetSection("Oracle"));
        Configure<BridgeOptions>(configuration.GetSection("Bridge"));
        context.Services.AddHostedService<EventHandlerAppHostedService>();
        context.Services.AddTransient(typeof(ILogEventProcessor<>), typeof(LogEventProcessorBase<>));
        context.Services.AddSingleton<ITransmitTransactionProvider, TransmitTransactionProvider>();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
    }
}