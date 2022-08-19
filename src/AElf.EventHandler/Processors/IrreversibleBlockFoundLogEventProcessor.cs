using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Bridge;
using AElf.Client.Core.Extensions;
using AElf.Client.Core.Options;
using AElf.Client.MerkleTreeContract;
using AElf.Client.Oracle;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MerkleTreeContract;
using AElf.Contracts.Oracle;
using AElf.Nethereum.Bridge;
using AElf.Nethereum.Core;
using AElf.Nethereum.Core.Options;
using AElf.Types;
using AutoMapper.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Math;
using BigInteger = System.Numerics.BigInteger;

namespace AElf.EventHandler;

public class IrreversibleBlockFoundLogEventProcessor : LogEventProcessorBase<IrreversibleBlockFound>
{
    private readonly AElfContractOptions _contractAddressOptions;
    private readonly BridgeOptions _bridgeOptions;
    private readonly EthereumContractOptions _ethereumContractOptions;
    private readonly OracleOptions _oracleOptions;
    private readonly ILatestQueriedReceiptCountProvider _latestQueriedReceiptCountProvider;
    private readonly BridgeService _bridgeContractService;
    private readonly IOracleService _oracleService;
    private readonly IBridgeOutService _bridgeOutService;
    private readonly INethereumService _nethereumService;
    private readonly IMerkleTreeContractService _merkleTreeContractService;
    private readonly ILogger<IrreversibleBlockFoundLogEventProcessor> _logger;
    private readonly string _lockAbi;

    public IrreversibleBlockFoundLogEventProcessor(
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        IOptionsSnapshot<BridgeOptions> bridgeOptions,
        IOptionsSnapshot<EthereumContractOptions> ethereumContractOptions,
        IOptionsSnapshot<OracleOptions> oracleOptions,
        ILatestQueriedReceiptCountProvider latestQueriedReceiptCountProvider,
        IOracleService oracleService,
        IBridgeOutService bridgeOutService,
        BridgeService bridgeService,
        IMerkleTreeContractService merkleTreeContractService,
        INethereumService nethereumService,
        ILogger<IrreversibleBlockFoundLogEventProcessor> logger) : base(contractAddressOptions)
    {
        _ethereumContractOptions = ethereumContractOptions.Value;
        _latestQueriedReceiptCountProvider = latestQueriedReceiptCountProvider;
        _oracleService = oracleService;
        _merkleTreeContractService = merkleTreeContractService;
        _logger = logger;
        _bridgeOptions = bridgeOptions.Value;
        _bridgeOutService = bridgeOutService;
        _oracleOptions = oracleOptions.Value;
        _contractAddressOptions = contractAddressOptions.Value;
        _bridgeContractService = bridgeService;
        _nethereumService = nethereumService;

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

    public override async Task ProcessAsync(LogEvent logEvent, EventContext context)
    {
        var libFound = new IrreversibleBlockFound();
        libFound.MergeFrom(logEvent);
        _logger.LogInformation($"IrreversibleBlockFound: {libFound}");

        if (!_bridgeOptions.IsSendQuery) return;
        var bridgeItemsMap = new Dictionary<(string, string), List<BridgeItem>>();
        var sendQueryList = new Dictionary<string, BridgeItem>();
        var tokenIndex = new Dictionary<string, BigInteger>();
        foreach (var bridgeItem in _bridgeOptions.Bridges)
        {
            bridgeItemsMap[(bridgeItem.EthereumClientAlias, bridgeItem.EthereumBridgeOutContractAddress)].Add(bridgeItem);
        }

        foreach (var (aliasAddress,item) in bridgeItemsMap)
        {
            var tokenList = item.Select(i => i.OriginToken).ToList();
            var targetChainIdList = item.Select(i => i.TargetChainId).ToList();
            var sendReceiptIndexDto = await _bridgeOutService.GetTransferReceiptIndexAsync(aliasAddress.Item1,aliasAddress.Item2,tokenList,targetChainIdList);
            for (var i = 0; i < tokenList.Count; i++)
            {
                var ethereumBlockNumber = await _nethereumService.GetBlockNumberAsync(aliasAddress.Item1);
                if (ethereumBlockNumber - sendReceiptIndexDto.BlockTimes[i] <= 12) continue;
                tokenIndex[tokenList[i]] = sendReceiptIndexDto.Indexes[i];
                sendQueryList[item[i].SwapId] = item[i];
            }
        }

        foreach (var (swapId,item) in sendQueryList)
        {
            await SendQueryAsync(item,tokenIndex[item.OriginToken]);
        }


    }

    private async Task QueryEthereumReceiptIndex(IGrouping<string,BridgeItem> item)
    {
    }
    private async Task SendQueryAsync(BridgeItem bridgeItem, BigInteger tokenIndex)
    {
        var swapId = bridgeItem.SwapId;
        var spaceId = await _bridgeContractService.GetSpaceIdBySwapIdAsync(Hash.LoadFromBase64(swapId));
        var lastRecordedLeafIndex = (await _merkleTreeContractService.GetLastLeafIndexAsync(
            new GetLastLeafIndexInput
            {
                SpaceId = spaceId
            })).Value;
        if (lastRecordedLeafIndex == -2)
        {
            _logger.LogInformation($"Space of id {spaceId} is not created. ");
            return;
        }
        if (_latestQueriedReceiptCountProvider.Get(swapId) == 0)
        {
            _latestQueriedReceiptCountProvider.Set(swapId,lastRecordedLeafIndex);
        }
        _logger.LogInformation($"Last recorded leaf index : {lastRecordedLeafIndex}.");
        if (tokenIndex <= _latestQueriedReceiptCountProvider.Get(swapId))
        {
            return;
        }
        
        var notRecordLeafCount = tokenIndex - lastRecordedLeafIndex;
        if (notRecordLeafCount > 0)
        {
            var queryInput = new QueryInput
            {
                Payment = _bridgeOptions.QueryPayment,
                QueryInfo = new QueryInfo
                {
                    Title = $"record_receipt_{swapId}",
                    Options = {(lastRecordedLeafIndex + 1).ToString(), notRecordLeafCount.ToString()}
                },
                AggregatorContractAddress = _contractAddressOptions.ContractAddressList["StringAggregator"]
                    .ConvertAddress(),
                CallbackInfo = new CallbackInfo
                {
                    ContractAddress =
                        _contractAddressOptions.ContractAddressList["Bridge"].ConvertAddress(),
                    MethodName = "RecordReceiptHash"
                },
                DesignatedNodeList = new AddressList
                {
                    Value = {bridgeItem.QueryToAddress.ConvertAddress()}
                }
            };

            _logger.LogInformation($"About to send Query transaction for token swapping, QueryInput: {queryInput}");

            var sendTxResult = await _oracleService.QueryAsync(queryInput);
            _logger.LogInformation($"Query tx id: {sendTxResult.Transaction.GetHash()}");
            _latestQueriedReceiptCountProvider.Set(swapId, (int) tokenIndex + 1);
            _logger.LogInformation(
                $"Latest queried receipt count: {_latestQueriedReceiptCountProvider.Get(swapId)}");
        }

        //Format Bridge Config => {ethereum contract address -> alias -> {token[],targetChainId[]}}
        // var bridgeGroup = _bridgeOptions.Bridges.GroupBy(e => e.EthereumBridgeInContractAddress).ToList();
        // foreach (var bridge in bridgeGroup)
        // {
        //     _logger.LogInformation($"Querying ethereum contract address : {bridge.Key}.");
        //     var bridgeGroupByAlias = bridge.GroupBy(e => e.EthereumClientAlias);
        //     foreach (var item in bridgeGroupByAlias)
        //     {
        //         var ethereumClientAlias = item.Key;
        //         var tokenList = item.Select(e => e.OriginToken).ToList();
        //         var targetChainIdList = item.Select(e => e.TargetChainId).ToList();
        //         var sendReceiptIndexDto = await _bridgeOutService.GetTransferReceiptIndexAsync(ethereumClientAlias,tokenList,targetChainIdList);
        //         
        //         var swapTokenMap = new Dictionary<string, BridgeItem>();
        //         foreach (var bridgeItem in item)
        //         {
        //             swapTokenMap[bridgeItem.SwapId] = bridgeItem;
        //         }
        //         var tokenIndex = new Dictionary<string, BigInteger>();
        //         var blockNumber = new Dictionary<string, BigInteger>();
        //         for (var i = 0; i < tokenList.Count; i++)
        //         {
        //             tokenIndex[tokenList[i]] = sendReceiptIndexDto.Indexes[i];
        //             blockNumber[tokenList[i]] = sendReceiptIndexDto.BlockTimes[i];
        //         }
        //         
        //         for (var i = 0; i < tokenList.Count; i++)
        //         {
        //             var ethereumBlockNumber = await _nethereumService.GetBlockNumberAsync(ethereumClientAlias);
        //             if (ethereumBlockNumber - blockNumber[tokenList[i]] < 12)
        //             {
        //                 tokenList.Remove(tokenList[i]);
        //             }
        //         }
        //         var swapTokenMapSend = swapTokenMap.Select(pair => tokenList.Contains(pair.Value.OriginToken) ? pair : default).ToList();
        //         foreach (var (swapId, value) in swapTokenMapSend)
        //         {
        //             var index  = tokenIndex[value.OriginToken];
        //             
        //
        //            
        //
        //         }
        //         
        //     }
        // }
    }
}