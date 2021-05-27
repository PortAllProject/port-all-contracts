using System.Threading.Tasks;
using AElf.Contracts.Oracle;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    internal class QueryCompletedLogEventProcessor : LogEventProcessorBase<QueryCompletedWithAggregation>,
        ITransientDependency
    {
        public override string ContractName => "Oracle";
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