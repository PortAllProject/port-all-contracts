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
    public class MiningInformationUpdatedLogEventProcessor : LogEventProcessorBase<MiningInformationUpdated>,
        ITransientDependency
    {
        private readonly ConfigOptions _configOptions;
        private readonly EthereumConfigOptions _ethereumConfigOptions;
        private readonly ContractAddressOptions _contractAddressOptions;
        private readonly ILogger<MiningInformationUpdatedLogEventProcessor> _logger;
        private readonly string _lockAbi;

        public MiningInformationUpdatedLogEventProcessor(
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions,
            IOptionsSnapshot<ConfigOptions> configOptions,
            IOptionsSnapshot<EthereumConfigOptions> ethereumConfigOptions,
            IOptionsSnapshot<ContractAbiOptions> contractAbiOptions,
            ILogger<MiningInformationUpdatedLogEventProcessor> logger) : base(contractAddressOptions)
        {
            var contractAbiOptions1 = contractAbiOptions.Value;
            _configOptions = configOptions.Value;
            _ethereumConfigOptions = ethereumConfigOptions.Value;
            _contractAddressOptions = contractAddressOptions.Value;

            {
                var file = contractAbiOptions1.LockAbiFilePath;
                if (!string.IsNullOrEmpty(file))
                    _lockAbi = JsonHelper.ReadJson(file, "abi");
            }

            _logger = logger;
        }

        public override string ContractName => "Consensus";

        public override async Task ProcessAsync(LogEvent logEvent)
        {
            var miningInformationUpdated = new MiningInformationUpdated();
            miningInformationUpdated.MergeFrom(logEvent);
            _logger.LogInformation($"Mining information updated: {miningInformationUpdated}");

            if (!_configOptions.SendQueryTransaction || miningInformationUpdated.Behaviour != "NextRound") return;

            var lockMappingContractAddress = _configOptions.LockMappingContractAddress;
            var merkleGeneratorContractAddress = _configOptions.MerkleGeneratorContractAddress;
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

            if (lockTimes > lastRecordedLeafIndex + 1)
            {
                node.SendTransaction(_configOptions.AccountAddress,
                    _contractAddressOptions.ContractAddressMap["Oracle"], "Query", new QueryInput
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
                    });
            }
        }
    }
}