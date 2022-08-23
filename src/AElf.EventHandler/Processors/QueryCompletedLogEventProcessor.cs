using System.Threading.Tasks;
using AElf.Client.Core.Extensions;
using AElf.Client.Core.Options;
using AElf.Contracts.Oracle;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler;

internal class QueryCompletedLogEventProcessor : LogEventProcessorBase<QueryCompletedWithAggregation>
{
    public override string ContractName => "OracleContract";
    private readonly ILogger<QueryCompletedLogEventProcessor> _logger;

    public QueryCompletedLogEventProcessor(ILogger<QueryCompletedLogEventProcessor> logger,
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions) : base(contractAddressOptions)
    {
        _logger = logger;
    }

    public override Task ProcessAsync(LogEvent logEvent, EventContext context)
    {
        var completed = new QueryCompletedWithAggregation();
        completed.MergeFrom(logEvent);
        _logger.LogInformation(logEvent.ToString());

        return Task.CompletedTask;
    }
}