using System.Threading.Tasks;
using AElf.Client.Core.Options;
using AElf.Contracts.IntegerAggregator;
using AElf.Types;
using Common.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    internal class AggregateDataReceivedLogEventProcessor : LogEventProcessorBase<AggregateDataReceived>, ITransientDependency
    {
        private readonly ILogger<AggregateDataReceivedLogEventProcessor> _logger;

        public AggregateDataReceivedLogEventProcessor(IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
            ILogger<AggregateDataReceivedLogEventProcessor> logger) : base(contractAddressOptions)
        {
            _logger = logger;
        }

        public override string ContractName => "IntegerAggregatorContract";

        public override Task ProcessAsync(LogEvent logEvent, EventContext context)
        {
            var aggregateDataReceived = new AggregateDataReceived();
            aggregateDataReceived.MergeFrom(logEvent);
            _logger.LogInformation($"AggregateDataReceived: {aggregateDataReceived}");

            return Task.CompletedTask;
        }
    }
}