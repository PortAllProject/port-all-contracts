using System;
using System.Net.Security;
using System.Security.Authentication;
using AElf.Contracts.Consensus.AEDPoS;
using Common.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.RabbitMQ;

namespace AElf.EventHandler
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(AbpEventBusRabbitMqModule)
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

            Configure<ContractAddressOptions>(configuration.GetSection("Contracts"));
            Configure<ConfigOptions>(configuration.GetSection("Config"));
            Configure<EthereumConfigOptions>(configuration.GetSection("Ethereum"));
            Configure<ContractAbiOptions>(configuration.GetSection("ContractAbi"));
            Configure<LotteryOptions>(configuration.GetSection("Lottery"));
            context.Services.AddHostedService<EventHandlerAppHostedService>();
            context.Services.AddTransient(typeof(ILogEventProcessor<>), typeof(LogEventProcessorBase<>));
        }
    }
}