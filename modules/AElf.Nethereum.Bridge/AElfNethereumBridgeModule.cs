using AElf.Nethereum.Core;
using AElf.Nethereum.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElf.Nethereum.Bridge;

[DependsOn(
    typeof(AElfNethereumClientModule)
)]
public class AElfNethereumBridgeModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
    }
}