using System.Linq;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Boilerplate.EventHandler
{
    public class ReportProposedLogEventProcessor : ILogEventProcessor, ITransientDependency
    {
        private readonly ContractAddressOptions _contractAddressOptions;
        private readonly ConfigOptions _configOptions;
        private readonly AElfKeyStore _keyStore;

        public string ContractName => "Report";
        public string LogEventName => nameof(ReportProposed);

        public ReportProposedLogEventProcessor(IOptionsSnapshot<ConfigOptions> configOptions,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions)
        {
            _configOptions = configOptions.Value;
            _contractAddressOptions = contractAddressOptions.Value;
            _keyStore = AElfKeyStore.GetKeyStore(CommonHelper.GetSignKeyDir());
        }

        public void Process(LogEvent logEvent)
        {
            var reportProposed = new ReportProposed();
            reportProposed.MergeFrom(logEvent);
            var publicKey = GetSignPublicKey();
            var keyPair = _keyStore.GetAccountKeyPair(publicKey);
            var signature = SignHelper.Sign(reportProposed.RawReport, keyPair.PrivateKey);
            var node = new NodeManager(_configOptions.BlockChainEndpoint);
            node.SendTransaction(_configOptions.AccountAddress,
                _contractAddressOptions.ContractAddressMap[ContractName], "ConfirmReport", new ConfirmReportInput
                {
                    EthereumContractAddress = _configOptions.EthereumContractAddress,
                    RoundId = reportProposed.RoundId,
                    Signature = signature.RecoverInfo
                });
        }
        private string GetSignPublicKey()
        {
            return AsyncHelper.RunSync(_keyStore.GetAccountsAsync).First();
        }
    }
}