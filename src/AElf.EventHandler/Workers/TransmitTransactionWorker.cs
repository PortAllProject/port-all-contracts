using System.Threading.Tasks;
using AElf.Client.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.EventHandler.Workers;

public class TransmitTransactionWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ITransmitTransactionProvider _transmitTransactionProvider;
    private readonly AElfChainAliasOptions _chainAliasOptions;

    public TransmitTransactionWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ITransmitTransactionProvider transmitTransactionProvider,
        IOptionsSnapshot<AElfChainAliasOptions>  chainAliasOptions) : base(timer,
        serviceScopeFactory)
    {
        _transmitTransactionProvider = transmitTransactionProvider;
        _chainAliasOptions = chainAliasOptions.Value;
        Timer.Period = 1000 * 60;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        foreach (var item in _chainAliasOptions.Mapping)
        {
            await _transmitTransactionProvider.UpdateQueueAsync(item.Key);
        }
    }
}