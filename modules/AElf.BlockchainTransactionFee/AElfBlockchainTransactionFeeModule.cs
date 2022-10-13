using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;

namespace AElf.BlockchainTransactionFee
{
    [DependsOn(typeof(AbpCachingModule))]
    public class AElfBlockchainTransactionFeeModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<ChainExplorerApiOptions>(configuration.GetSection("ChainExplorerApi"));            
            context.Services.AddTransient<ApiClient>();
            context.Services.AddTransient<IBlockchainTransactionFeeProvider, EthereumTransactionFeeProvider>();
            context.Services.AddTransient<IBlockchainTransactionFeeProvider, BSCTransactionFeeProvider>();
        }
    }
}