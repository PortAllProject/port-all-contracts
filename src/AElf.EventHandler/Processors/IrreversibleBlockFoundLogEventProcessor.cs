using System.IO;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Oracle;
using AElf.Contracts.OracleUser;
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
                    _logger.LogInformation($"abi: {_lockAbi}");
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
            var lastRecordedLeafIndex = node.QueryView<Int64Value>(_configOptions.AccountAddress,
                merkleTreeRecorderContractAddress, "GetLastRecordedLeafIndex",
                new RecorderIdInput
                {
                    RecorderId = _configOptions.RecorderId
                }).Value;

            _logger.LogInformation($"lock times: {lockTimes}; last recorded leaf index: {lastRecordedLeafIndex}");
            if (lockTimes >= lastRecordedLeafIndex + 1)
            {
                var queryInput = new QueryInput
                {
                    Payment = _configOptions.QueryPayment,
                    QueryInfo = new QueryInfo
                    {
                        Title = "swap"
                    },
                    AggregatorContractAddress = _contractAddressOptions.ContractAddressMap["StringAggregator"]
                        .ConvertAddress(),
                    CallbackInfo = new CallbackInfo
                    {
                        ContractAddress = _contractAddressOptions.ContractAddressMap["Bridge"].ConvertAddress(),
                        MethodName = "RecordMerkleTree"
                    },
                    DesignatedNodeList = new AddressList
                        {Value = {_configOptions.TokenSwapOracleOrganizationAddress.ConvertAddress()}}
                };

                _logger.LogInformation($"About to send Query transaction for token swapping, QueryInput: {queryInput}");

                node.SendTransaction(_configOptions.AccountAddress,
                    _contractAddressOptions.ContractAddressMap["Oracle"], "Query", queryInput);
            }
        }
    }
}