using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.EventHandler.Tests
{
    [DependsOn(typeof(EventHandlerAppModule))]
    public class AElfEventHandlerTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<QueryCreatedLogEventProcessor>();
            
            Configure<DataProviderOptions>(options =>
            {
                options.DataProviders[MockDataProvider.Title] = typeof(MockDataProvider);
            });
        }
    }
}