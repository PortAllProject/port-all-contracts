using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Oracle;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MTRecorder;

namespace AElf.EventHandler
{
    public class IrreversibleBlockFoundLogEventProcessor : LogEventProcessorBase<IrreversibleBlockFound>
    {
        private readonly ConfigOptions _configOptions;
        private readonly ContractAddressOptions _contractAddressOptions;
        private readonly INethereumManagerFactory _nethereumManagerFactory;
        private readonly ILogger<IrreversibleBlockFoundLogEventProcessor> _logger;

        private long _lastedQueryReceiptCount;

        public IrreversibleBlockFoundLogEventProcessor(
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions,
            IOptionsSnapshot<ConfigOptions> configOptions,
            INethereumManagerFactory nethereumManagerFactory,
            ILogger<IrreversibleBlockFoundLogEventProcessor> logger) : base(contractAddressOptions)
        {
            _nethereumManagerFactory = nethereumManagerFactory;
            _logger = logger;

            _configOptions = configOptions.Value;
            _contractAddressOptions = contractAddressOptions.Value;
        }

        public override string ContractName => "Consensus";

        public override async Task ProcessAsync(LogEvent logEvent)
        {
            var libFound = new IrreversibleBlockFound();
            libFound.MergeFrom(logEvent);
            _logger.LogInformation($"IrreversibleBlockFound: {libFound}");

            if (!_configOptions.SendQueryTransaction) return;

            var node = new NodeManager(_configOptions.BlockChainEndpoint, _configOptions.AccountAddress,
                _configOptions.AccountPassword);

            var receiptCountFunction = _nethereumManagerFactory.CreateManager(new LockMappingContractNameProvider())
                .GetFunction("receiptCount");
            var lockTimes = await receiptCountFunction.CallAsync<long>();
            if (lockTimes <= _lastedQueryReceiptCount)
            {
                return;
            }

            var lastRecordedLeafIndex = node.QueryView<Int64Value>(_configOptions.AccountAddress,
                _contractAddressOptions.ContractAddressMap["MTRecorder"], "GetLastRecordedLeafIndex",
                new RecorderIdInput
                {
                    RecorderId = _configOptions.RecorderId
                }).Value;

            if (lastRecordedLeafIndex == -2)
            {
                _logger.LogError($"Recorder of id {_configOptions.RecorderId} did created.");
                return;
            }

            _logger.LogInformation($"Lock times: {lockTimes}; Last recorded leaf index: {lastRecordedLeafIndex}");
            var notRecordedReceiptsCount = lockTimes - lastRecordedLeafIndex - 1;
            if (notRecordedReceiptsCount > 0)
            {
                var queryInput = new QueryInput
                {
                    Payment = _configOptions.QueryPayment,
                    QueryInfo = new QueryInfo
                    {
                        Title = "record_receipts",
                        Options = {(lastRecordedLeafIndex + 1).ToString(), (lockTimes - 1).ToString()}
                    },
                    AggregatorContractAddress = _contractAddressOptions.ContractAddressMap["StringAggregator"]
                        .ConvertAddress(),
                    CallbackInfo = new CallbackInfo
                    {
                        ContractAddress = _contractAddressOptions.ContractAddressMap["Bridge"].ConvertAddress(),
                        MethodName = "RecordReceiptHash"
                    },
                    DesignatedNodeList = new AddressList
                        {Value = {_configOptions.TokenSwapOracleOrganizationAddress.ConvertAddress()}}
                };

                _logger.LogInformation($"About to send Query transaction for token swapping, QueryInput: {queryInput}");

                var txId = node.SendTransaction(_configOptions.AccountAddress,
                    _contractAddressOptions.ContractAddressMap["Oracle"], "Query", queryInput);
                _logger.LogInformation($"Query tx id: {txId}");
                _lastedQueryReceiptCount = lockTimes;
            }
        }
    }
}