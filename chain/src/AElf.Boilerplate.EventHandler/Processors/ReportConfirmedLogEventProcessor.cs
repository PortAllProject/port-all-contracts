using System.Collections.Generic;
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
        private readonly HashSet<string> _signatures;

        public ReportConfirmedLogEventProcessor(ILogger<QueryCompletedLogEventProcessor> logger,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions) : base(contractAddressOptions)
        {
            _logger = logger;
            _signatures = new HashSet<string>();
        }

        public override Task ProcessAsync(LogEvent logEvent)
        {
            var reportConfirmed = new ReportConfirmed();
            reportConfirmed.MergeFrom(logEvent);
            _logger.LogInformation(reportConfirmed.ToString());
            _signatures.Add(reportConfirmed.Signature);
            if (reportConfirmed.IsAllNodeConfirm)
            {
                
                _signatures.Clear();
            }
            return Task.CompletedTask;
        }
    }
}