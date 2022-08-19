using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.EventHandler.Workers;

public class TransmitTransactionWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ITransmitTransactionProvider _transmitTransactionProvider;

    public TransmitTransactionWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ITransmitTransactionProvider transmitTransactionProvider) : base(timer,
        serviceScopeFactory)
    {
        _transmitTransactionProvider = transmitTransactionProvider;
        Timer.Period = 1000 * 60;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _transmitTransactionProvider.UpdateStatusAsync();
    }
}