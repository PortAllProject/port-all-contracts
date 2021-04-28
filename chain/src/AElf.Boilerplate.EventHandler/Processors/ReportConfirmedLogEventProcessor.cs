using System.Threading.Tasks;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public class ReportConfirmedLogEventProcessor : LogEventProcessorBase, ITransientDependency
    {
        public override string ContractName => "Report";
        public override string LogEventName => nameof(ReportConfirmed);
        private readonly ILogger<QueryCompletedLogEventProcessor> _logger;

        public ReportConfirmedLogEventProcessor(ILogger<QueryCompletedLogEventProcessor> logger,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions) : base(contractAddressOptions)
        {
            _logger = logger;
        }

        public override Task ProcessAsync(LogEvent logEvent)
        {
            var reportConfirmed = new ReportConfirmed();
            reportConfirmed.MergeFrom(logEvent);
            _logger.LogInformation(reportConfirmed.ToString());

            return Task.CompletedTask;
        }
    }
}