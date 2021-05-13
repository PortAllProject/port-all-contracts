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

            // Just for logging.
            Configure<MessageQueueOptions>(options =>
            {
                configuration.GetSection("MessageQueue").Bind(options);
            });

            Configure<AbpRabbitMqEventBusOptions>(options =>
            {
                var messageQueueConfig = configuration.GetSection("MessageQueue");
                options.ClientName = messageQueueConfig.GetSection("ClientName").Value;
                options.ExchangeName = messageQueueConfig.GetSection("ExchangeName").Value;
            });

            Configure<AbpRabbitMqOptions>(options =>
            {
                var messageQueueConfig = configuration.GetSection("MessageQueue");
                options.Connections.Default.HostName = messageQueueConfig.GetSection("HostName").Value;
                options.Connections.Default.Port = int.Parse(messageQueueConfig.GetSection("Port").Value);
                options.Connections.Default.UserName = messageQueueConfig.GetSection("UserName").Value;
                options.Connections.Default.Password = messageQueueConfig.GetSection("Password").Value;
            });

            Configure<ContractAddressOptions>(configuration.GetSection("Contracts"));
            Configure<ConfigOptions>(configuration.GetSection("Config"));
            Configure<EthereumConfigOptions>(configuration.GetSection("Ethereum"));
            context.Services.AddHostedService<EventHandlerAppHostedService>();
        }
    }
}