using System.IO;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Oracle;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MTRecorder;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public class IrreversibleBlockFoundLogEventProcessor : LogEventProcessorBase<IrreversibleBlockFound>,
        ITransientDependency
    {
        private readonly ConfigOptions _configOptions;
        private readonly EthereumConfigOptions _ethereumConfigOptions;
        private readonly ContractAddressOptions _contractAddressOptions;
        private readonly ILogger<IrreversibleBlockFoundLogEventProcessor> _logger;
        private readonly string _lockAbi;
        private readonly string _merkleAbi;

        private long _latestQueryReceiptCount = 0;

        public IrreversibleBlockFoundLogEventProcessor(
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions,
            IOptionsSnapshot<ConfigOptions> configOptions,
            IOptionsSnapshot<EthereumConfigOptions> ethereumConfigOptions,
            IOptionsSnapshot<ContractAbiOptions> contractAbiOptions,
            ILogger<IrreversibleBlockFoundLogEventProcessor> logger) : base(contractAddressOptions)
        {
            _logger = logger;

            var contractAbiOptions1 = contractAbiOptions.Value;
            _configOptions = configOptions.Value;
            _ethereumConfigOptions = ethereumConfigOptions.Value;
            _contractAddressOptions = contractAddressOptions.Value;

            {
                var file = contractAbiOptions1.LockAbiFilePath;
                if (!string.IsNullOrEmpty(file))
                {
                    if (!File.Exists(file))
                    {
                        _logger.LogError($"Cannot found file {file}");
                    }

                    _lockAbi = JsonHelper.ReadJson(file, "abi");
                    _logger.LogInformation($"Lock abi: {_lockAbi}");
                }
            }

            {
                var file = contractAbiOptions1.MerkleGeneratorAbiFilePath;
                if (!string.IsNullOrEmpty(file))
                {
                    if (!File.Exists(file))
                    {
                        _logger.LogError($"Cannot found file {file}");
                    }

                    _merkleAbi = JsonHelper.ReadJson(file, "abi");
                    _logger.LogInformation($"Merkle abi: {_merkleAbi}");
                }
            }
        }

        public override string ContractName => "Consensus";

        public override async Task ProcessAsync(LogEvent logEvent)
        {
            var libFound = new IrreversibleBlockFound();
            libFound.MergeFrom(logEvent);
            _logger.LogInformation($"IrreversibleBlockFound: {libFound}");

            if (!_configOptions.SendQueryTransaction) return;

            var lockMappingContractAddress = _configOptions.LockMappingContractAddress;
            var web3ManagerForLock = new Web3Manager(_ethereumConfigOptions.Url, lockMappingContractAddress,
                _ethereumConfigOptions.PrivateKey, _lockAbi);
            var node = new NodeManager(_configOptions.BlockChainEndpoint, _configOptions.AccountAddress,
                _configOptions.AccountPassword);
            var merkleTreeRecorderContractAddress = _contractAddressOptions.ContractAddressMap["MTRecorder"];

            var lockTimes = await web3ManagerForLock.GetFunction(lockMappingContractAddress, "receiptCount")
                .CallAsync<long>();

            var maxAvailableIndex = lockTimes - 1;
            if (_latestQueryReceiptCount == 0)
            {
                _latestQueryReceiptCount = node.QueryView<Int64Value>(_configOptions.AccountAddress,
                    merkleTreeRecorderContractAddress, "GetLastRecordedLeafIndex",
                    new RecorderIdInput
                    {
                        RecorderId = _configOptions.RecorderId
                    }).Value + 1;
            }
            var latestTreeIndex = _latestQueryReceiptCount / _configOptions.MaximumLeafCount;
            var almostTreeIndex = lockTimes / _configOptions.MaximumLeafCount;
            if (latestTreeIndex < almostTreeIndex)
            {
                maxAvailableIndex = (latestTreeIndex + 1) * _configOptions.MaximumLeafCount - 1;
            }

            _logger.LogInformation(
                $"Lock times: {lockTimes}; Latest tree index: {latestTreeIndex}; Almost tree index: {almostTreeIndex}; Max available index: {maxAvailableIndex};");

            if (maxAvailableIndex + 1 <= _latestQueryReceiptCount)
            {
                return;
            }

            var lastRecordedLeafIndex = node.QueryView<Int64Value>(_configOptions.AccountAddress,
                merkleTreeRecorderContractAddress, "GetLastRecordedLeafIndex",
                new RecorderIdInput
                {
                    RecorderId = _configOptions.RecorderId
                }).Value;

            if (lastRecordedLeafIndex == -2)
            {
                _logger.LogError($"Recorder of id {_configOptions.RecorderId} didn't created.");
                return;
            }

            _logger.LogInformation($"Last recorded leaf index: {lastRecordedLeafIndex}");
            var notRecordedReceiptsCount = maxAvailableIndex - lastRecordedLeafIndex;
            if (notRecordedReceiptsCount > 0)
            {
                var queryInput = new QueryInput
                {
                    Payment = _configOptions.QueryPayment,
                    QueryInfo = new QueryInfo
                    {
                        Title = "record_receipts",
                        Options = { (lastRecordedLeafIndex + 1).ToString(), maxAvailableIndex.ToString() }
                    },
                    AggregatorContractAddress = _contractAddressOptions.ContractAddressMap["StringAggregator"]
                        .ConvertAddress(),
                    CallbackInfo = new CallbackInfo
                    {
                        ContractAddress = _contractAddressOptions.ContractAddressMap["Bridge"].ConvertAddress(),
                        MethodName = "RecordReceiptHash"
                    },
                    DesignatedNodeList = new AddressList
                        { Value = { _configOptions.TokenSwapOracleOrganizationAddress.ConvertAddress() } }
                };

                _logger.LogInformation($"About to send Query transaction for token swapping, QueryInput: {queryInput}");

                var txId = node.SendTransaction(_configOptions.AccountAddress,
                    _contractAddressOptions.ContractAddressMap["Oracle"], "Query", queryInput);
                _logger.LogInformation($"Query tx id: {txId}");
                _latestQueryReceiptCount = maxAvailableIndex + 1;
                _logger.LogInformation($"_latestQueryReceiptCount: {_latestQueryReceiptCount}");
            }
        }
    }
}