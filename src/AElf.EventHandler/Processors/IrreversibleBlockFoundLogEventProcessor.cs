using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public class IrreversibleBlockFoundLogEventProcessor : LogEventProcessorBase<IrreversibleBlockFound>,
        ITransientDependency
    {
        private readonly ILogger<IrreversibleBlockFoundLogEventProcessor> _logger;

        public IrreversibleBlockFoundLogEventProcessor(IOptionsSnapshot<ContractAddressOptions> contractAddressOptions,
            ILogger<IrreversibleBlockFoundLogEventProcessor> logger) : base(contractAddressOptions)
        {
            _logger = logger;
        }

        public override string ContractName => "Consensus";

        public override Task ProcessAsync(LogEvent logEvent)
        {
            var libFound = new IrreversibleBlockFound();
            libFound.MergeFrom(logEvent);
            _logger.LogInformation($"IrreversibleBlockFound: {libFound}");

            return Task.CompletedTask;
        }
    }
}