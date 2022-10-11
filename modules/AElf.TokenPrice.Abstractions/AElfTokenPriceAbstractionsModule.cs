using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;

namespace AElf.TokenPrice
{
    public class AElfTokenPriceAbstractionsModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<TokenPriceOptions>(configuration.GetSection("TokenPrice"));
        }
    }
}