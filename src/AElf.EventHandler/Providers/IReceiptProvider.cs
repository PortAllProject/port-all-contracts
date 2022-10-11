using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.Client.Bridge;
using AElf.Client.Core.Options;
using AElf.Client.MerkleTreeContract;
using AElf.Client.Oracle;
using AElf.Contracts.MerkleTreeContract;
using AElf.Contracts.Oracle;
using AElf.EventHandler.Workers;
using AElf.Nethereum.Bridge;
using AElf.Nethereum.Core;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler;

public interface IReceiptProvider
{
    Task ExecuteAsync();
}

public class ReceiptProvider : IReceiptProvider, ITransientDependency
{
    private readonly BridgeOptions _bridgeOptions;
    private readonly IBridgeInService _bridgeInService;
    private readonly INethereumService _nethereumService;
    private readonly IOracleService _oracleService;
    private readonly AElfChainAliasOptions _aelfChainAliasOptions;
    private readonly IBridgeService _bridgeContractService;
    private readonly IMerkleTreeContractService _merkleTreeContractService;
    private readonly ILatestQueriedReceiptCountProvider _latestQueriedReceiptCountProvider;
    private readonly ILogger<ReceiptProvider> _logger;
    private readonly AElfContractOptions _contractOptions;
    private readonly BlockConfirmationOptions _blockConfirmationOptions;

    public ReceiptProvider(
        IOptionsSnapshot<BridgeOptions> bridgeOptions,
        IOptionsSnapshot<AElfChainAliasOptions> aelfChainAliasOption,
        IOptionsSnapshot<BlockConfirmationOptions> blockConfirmation,
        IOptionsSnapshot<AElfContractOptions> contractOptions,
        IBridgeInService bridgeInService,
        INethereumService nethereumService,
        IOracleService oracleService,
        IBridgeService bridgeService,
        IMerkleTreeContractService merkleTreeContractService,
        ILatestQueriedReceiptCountProvider latestQueriedReceiptCountProvider,
        ILogger<ReceiptProvider> logger)
    {
        _bridgeOptions = bridgeOptions.Value;
        _bridgeInService = bridgeInService;
        _nethereumService = nethereumService;
        _oracleService = oracleService;
        _bridgeContractService = bridgeService;
        _merkleTreeContractService = merkleTreeContractService;
        _aelfChainAliasOptions = aelfChainAliasOption.Value;
        _latestQueriedReceiptCountProvider = latestQueriedReceiptCountProvider;
        _logger = logger;
        _contractOptions = contractOptions.Value;
        _blockConfirmationOptions = blockConfirmation.Value;
    }

    public async Task ExecuteAsync()
    {
        var bridgeItemsMap = new Dictionary<(string, string), List<BridgeItemIn>>();
        var sendQueryList = new Dictionary<string, BridgeItemIn>();
        var tokenIndex = new Dictionary<(string, string), BigInteger>();
        foreach (var bridgeItem in _bridgeOptions.BridgesIn)
        {
            var key = (bridgeItem.ChainId, bridgeItem.EthereumBridgeInContractAddress);
            if (!bridgeItemsMap.TryGetValue(key, out var items))
            {
                items = new List<BridgeItemIn>();
            }

            items.Add(bridgeItem);
            bridgeItemsMap[key] = items;
        }

        foreach (var (aliasAddress, item) in bridgeItemsMap)
        {
            var tokenList = item.Select(i => i.OriginToken).ToList();
            var targetChainIdList = item.Select(i => i.TargetChainId).ToList();
            var tokenAndChainIdList = item.Select(i => (i.TargetChainId, i.OriginToken)).ToList();
            _logger.LogInformation($"chainId:{aliasAddress.Item1},ethereum bridgeIn address:{aliasAddress.Item2}");
            var sendReceiptIndexDto = await _bridgeInService.GetTransferReceiptIndexAsync(aliasAddress.Item1,
                aliasAddress.Item2, tokenList, targetChainIdList);
            for (var i = 0; i < tokenList.Count;i++)
            {
                _logger.LogInformation($"token:{tokenList[i]}-index:{sendReceiptIndexDto.Indexes[i]}");
            }
            for (var i = 0; i < tokenList.Count; i++)
            {
                _logger.LogInformation($"token and chain id:{tokenAndChainIdList[i].TargetChainId}{tokenAndChainIdList[i].OriginToken}-index:{sendReceiptIndexDto.Indexes[i]}");
                tokenIndex[tokenAndChainIdList[i]] = sendReceiptIndexDto.Indexes[i];
                sendQueryList[item[i].SwapId] = item[i];
            }
        }

        foreach (var (swapId, item) in sendQueryList)
        {
            var targetChainId = _bridgeOptions.BridgesIn.Single(i => i.SwapId == swapId).TargetChainId;
            _logger.LogInformation($"targetChainId:{targetChainId},chain id:{item.ChainId},bridge item token:{item.OriginToken},tokenIndex:{tokenIndex[(item.TargetChainId, item.OriginToken)]}");
            await SendQueryAsync(targetChainId, item, tokenIndex[(item.TargetChainId, item.OriginToken)]);
        }
    }

    private async Task SendQueryAsync(string chainId, BridgeItemIn bridgeItem, BigInteger tokenIndex)
    {
        var swapId = bridgeItem.SwapId;

        var spaceId = await _bridgeContractService.GetSpaceIdBySwapIdAsync(chainId, Hash.LoadFromHex(swapId));
        var lastRecordedLeafIndex = (await _merkleTreeContractService.GetLastLeafIndexAsync(
            chainId, new GetLastLeafIndexInput
            {
                SpaceId = spaceId
            })).Value;
        if (lastRecordedLeafIndex == -1)
        {
            _logger.LogInformation($"Space of id {spaceId} is not created. ");
            return;
        }

        var nextTokenIndex = lastRecordedLeafIndex == -2 ? 1 : lastRecordedLeafIndex + 2;
        if (_latestQueriedReceiptCountProvider.Get(swapId) == 0)
        {
            _latestQueriedReceiptCountProvider.Set(DateTime.UtcNow, swapId, nextTokenIndex);
        }
        else if (_latestQueriedReceiptCountProvider.Get(swapId) != nextTokenIndex)
        {
            var receiptIndexNow = _latestQueriedReceiptCountProvider.Get(swapId);
            _logger.LogInformation(
                $"Latest queried receipt index : {receiptIndexNow}, Last recorded leaf index : {nextTokenIndex}, Wait.");
            return;
        }

        _logger.LogInformation(
            $"{bridgeItem.ChainId}-{bridgeItem.TargetChainId}-{bridgeItem.OriginToken} Last recorded leaf index : {lastRecordedLeafIndex}.");

        var nextRoundStartTokenIndex = _latestQueriedReceiptCountProvider.Get(swapId);
        _logger.LogInformation(
            $"{bridgeItem.ChainId}-{bridgeItem.TargetChainId}-{bridgeItem.OriginToken} Next round to query should begin with receipt Index:{nextRoundStartTokenIndex}");


        if (tokenIndex < nextRoundStartTokenIndex)
        {
            return;
        }

        var notRecordTokenNumber = tokenIndex - nextRoundStartTokenIndex + 1;
        if (notRecordTokenNumber > 0)
        {
            var blockNumber = await _nethereumService.GetBlockNumberAsync(bridgeItem.ChainId);
            _logger.LogInformation(
                $"Input:ChainId:{bridgeItem.ChainId};BridgeInAddress:{bridgeItem.EthereumBridgeInContractAddress};OriginToken:{bridgeItem.OriginToken};TargetChainId:{bridgeItem.TargetChainId};nextRoundStartTokenIndex:{nextRoundStartTokenIndex};tokenIndex:{(long)tokenIndex}");
            var getReceiptInfos = await _bridgeInService.GetSendReceiptInfosAsync(bridgeItem.ChainId,
                bridgeItem.EthereumBridgeInContractAddress, bridgeItem.OriginToken, bridgeItem.TargetChainId,
                nextRoundStartTokenIndex, (long) tokenIndex);
            var lastTokenIndexConfirm = nextRoundStartTokenIndex - 1;
            string receiptIdHash = null;
            for (var i = 0; i < notRecordTokenNumber; i++)
            {
                var blockHeight = getReceiptInfos.Receipts[i].BlockHeight;
                receiptIdHash = getReceiptInfos.Receipts[i].ReceiptId.Split(".").First();
                var blockConfirmationCount = _blockConfirmationOptions.ConfirmationCount[bridgeItem.ChainId];
                if (blockNumber - blockHeight > blockConfirmationCount)
                {
                    lastTokenIndexConfirm = (i + nextRoundStartTokenIndex);
                    continue;
                }

                break;
            }

            _logger.LogInformation(
                $"{bridgeItem.ChainId}-{bridgeItem.TargetChainId}-{bridgeItem.OriginToken} Last confirmed receipt index:{lastTokenIndexConfirm}");

            _logger.LogInformation(
                $"{bridgeItem.ChainId}-{bridgeItem.TargetChainId}-{bridgeItem.OriginToken} Token hash in receipt id:{receiptIdHash}");

            if (lastTokenIndexConfirm - nextRoundStartTokenIndex >= 0)
            {
                _logger.LogInformation(
                    $"{bridgeItem.ChainId}-{bridgeItem.TargetChainId}-{bridgeItem.OriginToken} Start to query token : from receipt index {nextRoundStartTokenIndex},end receipt index {lastTokenIndexConfirm}");
                var queryInput = new QueryInput
                {
                    Payment = _bridgeOptions.QueryPayment,
                    QueryInfo = new QueryInfo
                    {
                        Title = $"record_receipts_{swapId}",
                        Options =
                        {
                            $"{receiptIdHash}.{nextRoundStartTokenIndex}", $"{receiptIdHash}.{lastTokenIndexConfirm}"
                        }
                    },
                    AggregatorContractAddress =
                        _contractOptions.ContractAddressList[chainId]["StringAggregatorContract"].ConvertAddress(),
                    CallbackInfo = new CallbackInfo
                    {
                        ContractAddress =
                            _contractOptions.ContractAddressList[chainId]["BridgeContract"].ConvertAddress(),
                        MethodName = "RecordReceiptHash"
                    },
                    DesignatedNodeList = new AddressList
                    {
                        Value = {bridgeItem.QueryToAddress.ConvertAddress()}
                    }
                };

                _logger.LogInformation(
                    $"{bridgeItem.ChainId}-{bridgeItem.TargetChainId}-{bridgeItem.OriginToken} About to send Query transaction for token swapping, QueryInput: {queryInput}");
                _latestQueriedReceiptCountProvider.Set(DateTime.UtcNow, swapId, lastTokenIndexConfirm + 1);
                var sendTxResult = await _oracleService.QueryAsync(chainId, queryInput);
                _logger.LogInformation(
                    $"{bridgeItem.ChainId}-{bridgeItem.TargetChainId}-{bridgeItem.OriginToken} Query transaction id : {sendTxResult.TransactionResult.TransactionId}");

                _logger.LogInformation(
                    $"{bridgeItem.ChainId}-{bridgeItem.TargetChainId}-{bridgeItem.OriginToken} Next receipt index should start with: {_latestQueriedReceiptCountProvider.Get(swapId)}");
            }
        }
    }
}