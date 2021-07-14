using System;
using System.Collections.Generic;
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
using Nethereum.ABI.FunctionEncoding.Attributes;
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
            var merkleContractAddress = _configOptions.MerkleGeneratorContractAddress;
            var web3ManagerForLock = new Web3Manager(_ethereumConfigOptions.Url, lockMappingContractAddress,
                _ethereumConfigOptions.PrivateKey, _lockAbi);
            var web3ManagerForMerkle = new Web3Manager(_ethereumConfigOptions.Url, merkleContractAddress,
                _ethereumConfigOptions.PrivateKey, _merkleAbi);
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

            _logger.LogInformation($"Lock times: {lockTimes}; Last recorded leaf index: {lastRecordedLeafIndex}");
            var notRecordedReceiptsCount = lockTimes - lastRecordedLeafIndex - 1;
            if (notRecordedReceiptsCount > 0)
            {
                var receiptInfoFunction =
                    web3ManagerForLock.GetFunction(_ethereumConfigOptions.Address, "getReceiptInfo");
                for (var i = lastRecordedLeafIndex + 1; i < lockTimes; i++)
                {
                    var receiptInfo = await receiptInfoFunction.CallAsync<ReceiptInfo>();
                    _logger.LogInformation(
                        $"Receipt: {receiptInfo.ReceiptId.ToHex()}, {receiptInfo.TargetAddress}, {receiptInfo.Amount}");
                }

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

        [FunctionOutput]
        public class ReceiptInfo : IFunctionOutputDTO
        {
            [Parameter("bytes32", 1)] public byte[] ReceiptId { get; set; }

            [Parameter("string", 2)] public string TargetAddress { get; set; }

            [Parameter("uint256", 3)] public long Amount { get; set; }
        }
    }
}