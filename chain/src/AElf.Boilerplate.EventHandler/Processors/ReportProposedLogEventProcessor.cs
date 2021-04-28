using System.Threading.Tasks;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public class ReportProposedLogEventProcessor : LogEventProcessorBase, ITransientDependency
    {
        private readonly ContractAddressOptions _contractAddressOptions;
        private readonly ConfigOptions _configOptions;
        private readonly IKeyStore _keyStore;

        public override string ContractName => "Report";
        public override string LogEventName => nameof(ReportProposed);

        public ReportProposedLogEventProcessor(IOptionsSnapshot<ConfigOptions> configOptions,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions) : base(contractAddressOptions)
        {
            _configOptions = configOptions.Value;
            _contractAddressOptions = contractAddressOptions.Value;
            _keyStore = AElfKeyStore.GetKeyStore(configOptions.Value.AccountAddress,
                configOptions.Value.AccountPassword);
        }

        public override Task ProcessAsync(LogEvent logEvent)
        {
            var reportProposed = new ReportProposed();
            reportProposed.MergeFrom(logEvent);
            var node = new NodeManager(_configOptions.BlockChainEndpoint, _configOptions.AccountAddress,
                _configOptions.AccountPassword);
            node.SendTransaction(_configOptions.AccountAddress,
                _contractAddressOptions.ContractAddressMap[ContractName], "ConfirmReport", new ConfirmReportInput
                {
                    EthereumContractAddress = _configOptions.EthereumContractAddress,
                    RoundId = reportProposed.RoundId,
                    Signature = SignHelper
                        .GetSignature(reportProposed.RawReport, _keyStore.GetAccountKeyPair().PrivateKey).RecoverInfo
                });

            return Task.CompletedTask;
        }
    }
}