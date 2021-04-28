using System.Threading.Tasks;
using AElf.Standards.ACS0;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public class ContractDeployedLogEventProcessor : ILogEventProcessor, ITransientDependency
    {
        public string ContractName => "BasicZero";
        public string LogEventName => nameof(ContractDeployed);
        private readonly ILogger<QueryCreatedLogEventProcessor> _logger;

        public ContractDeployedLogEventProcessor(ILogger<QueryCreatedLogEventProcessor> logger)
        {
            _logger = logger;
        }

        public Task ProcessAsync(LogEvent logEvent)
        {
            var contractDeployed = new ContractDeployed();
            contractDeployed.MergeFrom(logEvent);
            _logger.LogInformation($"New contract deployed: {contractDeployed}");

            return Task.CompletedTask;
        }
    }
}