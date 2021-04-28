using System.Threading.Tasks;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public class ReportConfirmedLogEventProcessor : ILogEventProcessor, ITransientDependency
    {
        public string ContractName => "Report";
        public string LogEventName => nameof(ReportConfirmed);
        private readonly ILogger<QueryCompletedLogEventProcessor> _logger;

        public ReportConfirmedLogEventProcessor(ILogger<QueryCompletedLogEventProcessor> logger)
        {
            _logger = logger;
        }

        public Task ProcessAsync(LogEvent logEvent)
        {
            var reportConfirmed = new ReportConfirmed();
            reportConfirmed.MergeFrom(logEvent);
            _logger.LogInformation(reportConfirmed.ToString());

            return Task.CompletedTask;
        }
    }
}