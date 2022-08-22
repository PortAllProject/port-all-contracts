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
using AElf.Nethereum.Bridge;
using AElf.Nethereum.Core;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.RPC.Eth.Blocks;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AElf.EventHandler.Workers;

public class ReceiptSyncWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly BridgeOptions _bridgeOptions;
    private readonly BridgeOutService _bridgeOutService;
    private readonly NethereumService _nethereumService;
    private readonly OracleService _oracleService;
    private readonly AElfChainAliasOptions _aelfChainAliasOptions;
    private readonly BridgeService _bridgeContractService;
    private readonly IMerkleTreeContractService _merkleTreeContractService;
    private readonly ILatestQueriedReceiptCountProvider _latestQueriedReceiptCountProvider;
    private readonly ILogger<ReceiptSyncWorker> _logger;
    private readonly AElfContractOptions _contractOptions;
    private readonly BlockConfirmationOptions _blockConfirmationOptions;

    public ReceiptSyncWorker(AbpAsyncTimer timer,
        IServiceScopeFactory serviceScopeFactory,
        IOptionsSnapshot<BridgeOptions> bridgeOptions,
        IOptionsSnapshot<AElfChainAliasOptions> aelfChainAliasOption,
        IOptionsSnapshot<BlockConfirmationOptions> blockConfirmation,
        BridgeOutService bridgeOutService,
        NethereumService nethereumService,
        OracleService oracleService,
        BridgeService bridgeService,
        IMerkleTreeContractService merkleTreeContractService,
        ILatestQueriedReceiptCountProvider latestQueriedReceiptCountProvider,
        ILogger<ReceiptSyncWorker> logger,
        AElfContractOptions contractOptions) : base(timer,
        serviceScopeFactory)
    {
        Timer.Period = 1000 * 60;
        _bridgeOptions = bridgeOptions.Value;
        _bridgeOutService = bridgeOutService;
        _nethereumService = nethereumService;
        _oracleService = oracleService;
        _bridgeContractService = bridgeService;
        _merkleTreeContractService = merkleTreeContractService;
        _aelfChainAliasOptions = aelfChainAliasOption.Value;
        _latestQueriedReceiptCountProvider = latestQueriedReceiptCountProvider;
        _logger = logger;
        _contractOptions = contractOptions;
        _blockConfirmationOptions = blockConfirmation.Value;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        if (!_bridgeOptions.IsSendQuery) return;
        var bridgeItemsMap = new Dictionary<(string, string), List<BridgeItem>>();
        var sendQueryList = new Dictionary<string, BridgeItem>();
        var tokenIndex = new Dictionary<string, BigInteger>();
        foreach (var bridgeItem in _bridgeOptions.Bridges)
        {
            bridgeItemsMap[(bridgeItem.EthereumClientAlias, bridgeItem.EthereumBridgeOutContractAddress)]
                .Add(bridgeItem);
        }

        foreach (var (aliasAddress, item) in bridgeItemsMap)
        {
            var tokenList = item.Select(i => i.OriginToken).ToList();
            var targetChainIdList = item.Select(i => i.TargetChainId).ToList();
            var sendReceiptIndexDto = await _bridgeOutService.GetTransferReceiptIndexAsync(aliasAddress.Item1,
                aliasAddress.Item2, tokenList, targetChainIdList);
            for (var i = 0; i < tokenList.Count; i++)
            {
                tokenIndex[tokenList[i]] = sendReceiptIndexDto.Indexes[i];
                sendQueryList[item[i].SwapId] = item[i];
            }
        }

        foreach (var (swapId, item) in sendQueryList)
        {
            var targetChainId = _bridgeOptions.Bridges.Single(i => i.SwapId == swapId).TargetChainId;
            await SendQueryAsync(targetChainId, item, tokenIndex[item.OriginToken]);
        }
    }

    private async Task SendQueryAsync(string chainId, BridgeItem bridgeItem, BigInteger tokenIndex)
    {
        var swapId = bridgeItem.SwapId;
        var clientAlias = _aelfChainAliasOptions.Mapping[chainId];
        var spaceId = await _bridgeContractService.GetSpaceIdBySwapIdAsync(clientAlias, Hash.LoadFromBase64(swapId));
        var lastRecordedLeafIndex = (await _merkleTreeContractService.GetLastLeafIndexAsync(
            clientAlias, new GetLastLeafIndexInput
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
            _latestQueriedReceiptCountProvider.Set(swapId, lastRecordedLeafIndex);
        }

        _logger.LogInformation($"Last recorded leaf index : {lastRecordedLeafIndex}.");
        if (tokenIndex <= _latestQueriedReceiptCountProvider.Get(swapId))
        {
            return;
        }
        
        if (tokenIndex - lastRecordedLeafIndex > 0)
        {
            var blockNumber = await _nethereumService.GetBlockNumberAsync(bridgeItem.EthereumClientAlias);
            var getReceiptInfos = await _bridgeOutService.GetSendReceiptInfosAsync(_aelfChainAliasOptions.Mapping[chainId],
                bridgeItem.EthereumBridgeOutContractAddress, bridgeItem.OriginToken, bridgeItem.TargetChainId,
                lastRecordedLeafIndex + 1,(long)tokenIndex);
            var lastTokenIndexConfirm = lastRecordedLeafIndex;
            for (var i = 0; i < lastRecordedLeafIndex - tokenIndex; i++)
            {
                var blockHeight = getReceiptInfos.Receipts[i].BlockHeight;
                var blockConfirmationCount = _blockConfirmationOptions.ConfirmationCount[bridgeItem.EthereumClientAlias];
                if (blockNumber - blockHeight > blockConfirmationCount) continue;
                lastTokenIndexConfirm += (i+1);
                break;
            }

            if (lastTokenIndexConfirm - lastRecordedLeafIndex > 0)
            {
                var queryInput = new QueryInput
                {
                    Payment = _bridgeOptions.QueryPayment,
                    QueryInfo = new QueryInfo
                    {
                        Title = $"record_receipt_{swapId}",
                        Options = {(lastRecordedLeafIndex + 1).ToString(), lastTokenIndexConfirm.ToString()}
                    },
                    AggregatorContractAddress = _contractOptions.ContractAddressList["StringAggregator"]
                        .ConvertAddress(),
                    CallbackInfo = new CallbackInfo
                    {
                        ContractAddress =
                            _contractOptions.ContractAddressList["Bridge"].ConvertAddress(),
                        MethodName = "RecordReceiptHash"
                    },
                    DesignatedNodeList = new AddressList
                    {
                        Value = {bridgeItem.QueryToAddress.ConvertAddress()}
                    }
                };

                _logger.LogInformation($"About to send Query transaction for token swapping, QueryInput: {queryInput}");

                var sendTxResult = await _oracleService.QueryAsync(clientAlias, queryInput);
                _logger.LogInformation($"Query tx id: {sendTxResult.Transaction.GetHash()}");
                _latestQueriedReceiptCountProvider.Set(swapId, (int) tokenIndex + 1);
                _logger.LogInformation(
                    $"Latest queried receipt count: {_latestQueriedReceiptCountProvider.Get(swapId)}");
            }
            
        }
    }
}