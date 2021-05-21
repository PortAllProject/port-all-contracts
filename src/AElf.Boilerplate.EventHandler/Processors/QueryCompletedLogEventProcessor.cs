using System.Threading.Tasks;
using AElf.Contracts.Oracle;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public class QueryCompletedLogEventProcessor : LogEventProcessorBase, ITransientDependency
    {
        public override string ContractName => "Oracle";
        public override string LogEventName => nameof(QueryCompletedWithAggregation);
        private readonly ILogger<QueryCompletedLogEventProcessor> _logger;

        public QueryCompletedLogEventProcessor(ILogger<QueryCompletedLogEventProcessor> logger,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions) : base(contractAddressOptions)
        {
            _logger = logger;
        }

        public override Task ProcessAsync(LogEvent logEvent)
        {
            var completed = new QueryCompletedWithAggregation();
            completed.MergeFrom(logEvent);
            _logger.LogInformation(logEvent.ToString());

            return Task.CompletedTask;
        }
    }
}