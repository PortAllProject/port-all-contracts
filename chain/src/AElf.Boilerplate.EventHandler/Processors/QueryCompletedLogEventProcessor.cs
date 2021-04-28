using System.Threading.Tasks;
using AElf.Contracts.Oracle;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public class QueryCompletedLogEventProcessor : ILogEventProcessor, ITransientDependency
    {
        public string ContractName => "Oracle";
        public string LogEventName => nameof(QueryCompleted);
        private readonly ILogger<QueryCompletedLogEventProcessor> _logger;

        public QueryCompletedLogEventProcessor(ILogger<QueryCompletedLogEventProcessor> logger)
        {
            _logger = logger;
        }

        public Task ProcessAsync(LogEvent logEvent)
        {
            var collected = new QueryCompleted();
            collected.MergeFrom(logEvent);
            _logger.LogInformation(logEvent.ToString());

            return Task.CompletedTask;
        }
    }
}