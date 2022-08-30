using System;
using System.Threading.Tasks;
using AElf.BlockchainTransactionFee;
using AElf.Client.Bridge;
using AElf.Contracts.Bridge;
using AElf.TokenPrice;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.PriceWorker;

public class PriceSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly PriceSyncOptions _priceSyncOptions;
    private readonly IBridgeService _bridgeService;
    private readonly IBlockchainTransactionFeeService _blockchainTransactionFeeService;
    private readonly ITokenPriceService _tokenPriceService;

    public PriceSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IOptionsSnapshot<PriceSyncOptions> priceSyncOptions, IBridgeService bridgeService,
        IBlockchainTransactionFeeService blockchainTransactionFeeService, ITokenPriceService tokenPriceService) : base(
        timer, serviceScopeFactory)
    {
        _bridgeService = bridgeService;
        _blockchainTransactionFeeService = blockchainTransactionFeeService;
        _tokenPriceService = tokenPriceService;
        _priceSyncOptions = priceSyncOptions.Value;

        Timer.Period = 1000 * 1;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Logger.LogInformation($"================");
        var setGasPriceInput = new SetGasPriceInput();
        var setPriceRatioInput = new SetPriceRatioInput();
        
        var elfPrice = await _tokenPriceService.GetPriceAsync("ELF");
        
        foreach (var item in _priceSyncOptions.SourceChains)
        {
            var gasFee = await _blockchainTransactionFeeService.GetTransactionFeeAsync(item.ChainType);
            var feeWei = (long)(gasFee.Fee * (decimal)Math.Pow(10, 9));
            setGasPriceInput.GasPriceList.Add(new GasPrice
            {
                ChainId = item.ChainId,
                GasPrice_ = feeWei
            });
        
            decimal nativePrice = await _tokenPriceService.GetPriceAsync(item.NativeToken);
            var ratio = (long)(nativePrice * (decimal)Math.Pow(10, 8) / elfPrice);
            setPriceRatioInput.Value.Add(new PriceRatio
            {
                TargetChainId = item.ChainId,
                PriceRatio_ = ratio
            });
        }

        foreach (var item in _priceSyncOptions.TargetChains)
        {
            await _bridgeService.SetGasPriceAsync(item, setGasPriceInput);
            await _bridgeService.SetPriceRatioAsync(item, setPriceRatioInput);
        }
    }
}