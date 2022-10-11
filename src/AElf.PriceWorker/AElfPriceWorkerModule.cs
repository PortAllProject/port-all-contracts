using System;
using System.Net;
using System.Net.Http;
using AElf.BlockchainTransactionFee;
using AElf.Client.Bridge;
using AElf.Client.Core;
using AElf.TokenPrice;
using AElf.TokenPrice.CoinGecko;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace AElf.PriceWorker;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AElfClientModule),
    typeof(AElfClientBridgeModule),
    typeof(AElfBlockchainTransactionFeeModule),
    typeof(AElfTokenPriceAbstractionsModule),
    typeof(AElfTokenPriceCoinGeckoModule),
    typeof(AbpBackgroundWorkersModule)
)]
public class AElfPriceWorkerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        var configuration = context.Services.GetConfiguration();
        Configure<PriceSyncOptions>(configuration.GetSection("PriceSync"));
        context.Services.AddHostedService<AElfPriceWorkerHostedService>();
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        context.AddBackgroundWorkerAsync<PriceSyncWorker>();
    }
}