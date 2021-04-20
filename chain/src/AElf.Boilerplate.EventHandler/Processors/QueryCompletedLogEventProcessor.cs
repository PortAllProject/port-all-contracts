using System;
using System.Threading.Tasks;
using AElf.Contracts.Oracle;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public class QueryCompletedLogEventProcessor : ILogEventProcessor, ITransientDependency
    {
        public string ContractName => "Oracle";
        public string LogEventName => nameof(SufficientDataCollected);

        public Task ProcessAsync(LogEvent logEvent)
        {
            var collected = new QueryCompleted();
            collected.MergeFrom(logEvent);
            Console.WriteLine(logEvent);

            return Task.CompletedTask;
        }
    }
}