using System.Threading.Tasks;
using AElf.Standards.ACS0;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.EventHandler
{
    internal class ContractDeployedLogEventProcessor : LogEventProcessorBase<ContractDeployed>
    {
        public override string ContractName => "BasicZero";
        private readonly ILogger<QueryCreatedLogEventProcessor> _logger;

        public ContractDeployedLogEventProcessor(ILogger<QueryCreatedLogEventProcessor> logger,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions) : base(contractAddressOptions)
        {
            _logger = logger;
        }

        public override Task ProcessAsync(LogEvent logEvent)
        {
            var contractDeployed = new ContractDeployed();
            contractDeployed.MergeFrom(logEvent);
            _logger.LogInformation($"New contract deployed: {contractDeployed}");

            return Task.CompletedTask;
        }
    }
}