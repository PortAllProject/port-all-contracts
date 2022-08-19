using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.EventHandler.Workers;

public class ReceiptSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    public ReceiptSyncWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory
        ) : base(timer,
        serviceScopeFactory)
    {
        Timer.Period = 1000 * 60;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
    }
}