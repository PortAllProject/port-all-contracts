using AElf.Client.Core;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Client.Oracle;

[DependsOn(
    typeof(AElfClientModule),
    typeof(CoreAElfModule)
)]
public class AElfClientOracleModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<OracleOptions>(configuration.GetSection("Oracle"));
    }
}