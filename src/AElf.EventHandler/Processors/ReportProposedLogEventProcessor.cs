using System.Threading.Tasks;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    internal class ReportProposedLogEventProcessor : LogEventProcessorBase<ReportProposed>, ITransientDependency
    {
        private readonly ContractAddressOptions _contractAddressOptions;
        private readonly ConfigOptions _configOptions;
        private readonly IKeyStore _keyStore;
        private readonly IReportProvider _reportProvider;

        public override string ContractName => "Report";
        private readonly ILogger<ReportProposedLogEventProcessor> _logger;

        public ReportProposedLogEventProcessor(IOptionsSnapshot<ConfigOptions> configOptions,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions,
            IReportProvider reportProvider,
            ILogger<ReportProposedLogEventProcessor> logger) : base(contractAddressOptions)
        {
            _logger = logger;
            _configOptions = configOptions.Value;
            _contractAddressOptions = contractAddressOptions.Value;
            _keyStore = AElfKeyStore.GetKeyStore(configOptions.Value.AccountAddress,
                configOptions.Value.AccountPassword);
            _reportProvider = reportProvider;
        }

        public override Task ProcessAsync(LogEvent logEvent)
        {
            var reportProposed = new ReportProposed();
            reportProposed.MergeFrom(logEvent);

            _logger.LogInformation($"New report: {reportProposed}");

            var node = new NodeManager(_configOptions.BlockChainEndpoint, _configOptions.AccountAddress,
                _configOptions.AccountPassword);
            var txId = node.SendTransaction(_configOptions.AccountAddress,
                _contractAddressOptions.ContractAddressMap[ContractName], "ConfirmReport", new ConfirmReportInput
                {
                    Token = _configOptions.TransmitContractAddress,
                    RoundId = reportProposed.RoundId,
                    Signature = SignHelper
                        .GetSignature(reportProposed.RawReport, _keyStore.GetAccountKeyPair().PrivateKey).RecoverInfo
                });
            _reportProvider.SetReport(_configOptions.TransmitContractAddress, reportProposed.RoundId, reportProposed.RawReport);
            _logger.LogInformation($"[ConfirmReport] Tx id {txId}");

            return Task.CompletedTask;
        }
    }
}