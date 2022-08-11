using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Core.Extensions;
using AElf.Client.Core.Options;
using AElf.Client.MerkleTreeContract;
using AElf.Client.Oracle;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MerkleTreeContract;
using AElf.Contracts.Oracle;
using AElf.Nethereum.Core.Options;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.EventHandler;

public class IrreversibleBlockFoundLogEventProcessor : LogEventProcessorBase<IrreversibleBlockFound>
{
    private readonly ConfigOptions _configOptions;
    private readonly EthereumConfigOptions _ethereumConfigOptions;
    private readonly AElfContractOptions _contractAddressOptions;
    private readonly EthereumContractOptions _ethereumContractOptions;
    private readonly ILatestQueriedReceiptCountProvider _latestQueriedReceiptCountProvider;
    private readonly IOracleService _oracleService;
    private readonly IMerkleTreeContractService _merkleTreeContractService;
    private readonly ILogger<IrreversibleBlockFoundLogEventProcessor> _logger;
    private readonly string _lockAbi;

    public IrreversibleBlockFoundLogEventProcessor(
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        IOptionsSnapshot<ConfigOptions> configOptions,
        IOptionsSnapshot<EthereumConfigOptions> ethereumConfigOptions,
        IOptionsSnapshot<EthereumContractOptions> ethereumContractOptions,
        ILatestQueriedReceiptCountProvider latestQueriedReceiptCountProvider,
        IOracleService oracleService,
        IMerkleTreeContractService merkleTreeContractService,
        ILogger<IrreversibleBlockFoundLogEventProcessor> logger) : base(contractAddressOptions)
    {
        _ethereumContractOptions = ethereumContractOptions.Value;
        _latestQueriedReceiptCountProvider = latestQueriedReceiptCountProvider;
        _oracleService = oracleService;
        _merkleTreeContractService = merkleTreeContractService;
        _logger = logger;

        _configOptions = configOptions.Value;
        _ethereumConfigOptions = ethereumConfigOptions.Value;
        _contractAddressOptions = contractAddressOptions.Value;

        {
            var file = Path.Combine(_ethereumContractOptions.AbiFileDirectory,
                _ethereumContractOptions.ContractInfoList["Bridge"].AbiFileName);
            if (!string.IsNullOrEmpty(file))
            {
                if (!File.Exists(file))
                {
                    _logger.LogError($"Cannot found file {file}");
                }

                _lockAbi = JsonHelper.ReadJson(file, "abi");
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

        foreach (var swapConfig in _configOptions.SwapConfigs)
        {
            await SendQueryAsync(swapConfig.LockMappingContractAddress, Hash.LoadFromBase64(swapConfig.SpaceId),
                swapConfig.TokenSymbol);
        }
    }

    private async Task SendQueryAsync(string lockMappingContractAddress, Hash spaceId, string symbol)
    {
        _logger.LogInformation($"Querying {lockMappingContractAddress}, Space Id {spaceId}, Symbol {symbol}");
        var nodeUrl = _configOptions.SwapConfigs.Single(c => Hash.LoadFromBase64(c.SpaceId) == spaceId).NodeUrl;
        var web3ManagerForLock = new Web3Manager(nodeUrl, lockMappingContractAddress,
            "", _lockAbi);

        // TODO: Travel Bridge.Bridges config.

        var lockTimes = await web3ManagerForLock.GetFunction(lockMappingContractAddress, "receiptCount")
            .CallAsync<long>();// TODO: Bridges.OriginToken

        _logger.LogInformation($"{symbol} lock times: {lockTimes}");

        var lastRecordedLeafIndex = (await _merkleTreeContractService.GetLastLeafIndexAsync(
            new GetLastLeafIndexInput
            {
                SpaceId = spaceId
            })).Value;

        var maxAvailableIndex = lockTimes - 1;
        if (_latestQueriedReceiptCountProvider.Get(symbol) == 0)
        {
            _latestQueriedReceiptCountProvider.Set(symbol, lastRecordedLeafIndex + 1);
        }

        var latestTreeIndex = _latestQueriedReceiptCountProvider.Get(symbol) / _configOptions.MaximumLeafCount;
        _logger.LogInformation($"{symbol} latest tree index: {latestTreeIndex}");

        var almostTreeIndex = lockTimes / _configOptions.MaximumLeafCount;
        if (latestTreeIndex < almostTreeIndex)
        {
            maxAvailableIndex = (latestTreeIndex + 1) * _configOptions.MaximumLeafCount - 1;
        }

        _logger.LogInformation(
            $"Lock times: {lockTimes}; Latest tree index: {latestTreeIndex}; Almost tree index: {almostTreeIndex}; Max available index: {maxAvailableIndex};");

        if (maxAvailableIndex + 1 <= _latestQueriedReceiptCountProvider.Get(symbol))
        {
            return;
        }

        if (lastRecordedLeafIndex == -2)
        {
            _logger.LogError($"Space {spaceId.ToHex()} didn't created.");
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
                    Title = $"record_receipts_{symbol}",// TODO: Change this to SwapId.
                    Options = { (lastRecordedLeafIndex + 1).ToString(), maxAvailableIndex.ToString() }
                },
                AggregatorContractAddress = _contractAddressOptions.ContractAddressList["StringAggregator"]
                    .ConvertAddress(),
                CallbackInfo = new CallbackInfo
                {
                    ContractAddress = _contractAddressOptions.ContractAddressList["Bridge"].ConvertAddress(),
                    MethodName = "RecordReceiptHash"
                },
                DesignatedNodeList = new AddressList
                    { Value = { _configOptions.TokenSwapOracleOrganizationAddress.ConvertAddress() } }
            };

            _logger.LogInformation($"About to send Query transaction for token swapping, QueryInput: {queryInput}");

            var sendTxResult = await _oracleService.QueryAsync(queryInput);
            _logger.LogInformation($"Query tx id: {sendTxResult.Transaction.GetHash()}");
            _latestQueriedReceiptCountProvider.Set(symbol, maxAvailableIndex + 1);
            _logger.LogInformation(
                $"Latest queried receipt count: {_latestQueriedReceiptCountProvider.Get(symbol)}");
        }
    }
}