using AElf.Nethereum.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElf.Nethereum.Core;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpAutoMapperModule)
)]
public class AElfNethereumClientModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();

        Configure<EthereumAccountOptions>(configuration.GetSection("EthereumAccount"));
        Configure<EthereumClientConfigOptions>(configuration.GetSection("EthereumClientConfig"));
        Configure<EthereumContractOptions>(configuration.GetSection("EthereumContract"));
        Configure<EthereumChainAliasOptions>(configuration.GetSection("EthereumChainAlias"));
        Configure<EthereumClientOptions>(configuration.GetSection("EthereumClient"));
    }
}