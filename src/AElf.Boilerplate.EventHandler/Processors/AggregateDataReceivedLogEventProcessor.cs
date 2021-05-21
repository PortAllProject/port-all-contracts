using System.Threading.Tasks;
using AElf.Contracts.IntegerAggregator;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public class AggregateDataReceivedLogEventProcessor : LogEventProcessorBase, ITransientDependency
    {
        private readonly ILogger<AggregateDataReceivedLogEventProcessor> _logger;

        public AggregateDataReceivedLogEventProcessor(IOptionsSnapshot<ContractAddressOptions> contractAddressOptions,
            ILogger<AggregateDataReceivedLogEventProcessor> logger) : base(contractAddressOptions)
        {
            _logger = logger;
        }

        public override string ContractName => "IntegerAggregator";

        public override Task ProcessAsync(LogEvent logEvent)
        {
            var aggregateDataReceived = new AggregateDataReceived();
            aggregateDataReceived.MergeFrom(logEvent);
            _logger.LogInformation($"AggregateDataReceived: {aggregateDataReceived}");

            return Task.CompletedTask;
        }

        public override string LogEventName => nameof(AggregateDataReceived);
    }
}