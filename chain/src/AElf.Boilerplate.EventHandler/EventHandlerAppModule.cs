using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.RabbitMQ;

namespace AElf.Boilerplate.EventHandler
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

            var messageQueueOptions = new MessageQueueOptions();

            Configure<MessageQueueOptions>(options =>
            {
                configuration.GetSection("MessageQueue").Bind(options);
                messageQueueOptions = options;
            });

            Configure<AbpRabbitMqEventBusOptions>(options =>
            {
                options.ClientName = messageQueueOptions.ClientName;
                options.ExchangeName = messageQueueOptions.ExchangeName;
            });

            Configure<AbpRabbitMqOptions>(options =>
            {
                options.Connections.Default.HostName = messageQueueOptions.HostName;
                options.Connections.Default.Port = messageQueueOptions.Port;
            });

            Configure<ContractAddressOptions>(configuration.GetSection("Contracts"));
            Configure<ConfigOptions>(configuration.GetSection("Config"));
            Configure<EthereumConfigOptions>(configuration.GetSection("Ethereum"));
            context.Services.AddHostedService<EventHandlerAppHostedService>();
        }
    }
}