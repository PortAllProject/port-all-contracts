using System;
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
            var hostEnvironment = context.Services.GetSingletonInstance<IHostEnvironment>();

            Configure<AbpRabbitMqEventBusOptions>(options =>
            {
                options.ClientName = "AElfEventHandler" + Guid.NewGuid();
                options.ExchangeName = "AElfExchange";
            });

            Configure<AbpRabbitMqOptions>(options =>
            {
                options.Connections.Default.HostName = "localhost";
                options.Connections.Default.Port = 5672;
            });

            Configure<ContractAddressOptions>(configuration.GetSection("Contracts"));
            Configure<ConfigOptions>(configuration.GetSection("Config"));
            Configure<EthereumConfigOptions>(configuration.GetSection("Ethereum"));
            context.Services.AddHostedService<EventHandlerAppHostedService>();
        }
    }
}