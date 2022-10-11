using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.EventHandler.Workers;

public class ReceiptSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly BridgeOptions _bridgeOptions;
    private readonly IReceiptProvider _receiptProvider; 

    public ReceiptSyncWorker(AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory,
        IOptionsSnapshot<BridgeOptions> bridgeOptions,
        IReceiptProvider receiptProvider
    ) : base(timer,
        serviceScopeFactory)
    {
        Timer.Period = 1000 * 60;
        _bridgeOptions = bridgeOptions.Value;
        _receiptProvider = receiptProvider;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        if (!_bridgeOptions.IsSendQuery) return;
        await _receiptProvider.ExecuteAsync();
    }
}