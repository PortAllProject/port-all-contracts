using System;
using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Nethereum.Bridge;
using AElf.Nethereum.Core;
using AElf.Nethereum.Core.Options;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nethereum.BlockchainProcessing.BlockStorage.Entities.Mapping;
using StackExchange.Redis;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.EventHandler;

public interface ITransmitTransactionProvider
{
    Task EnqueueAsync(SendTransmitArgs args);
    Task SendByLibAsync(string chainId, string libHash, long libHeight);
    Task UpdateQueueAsync(string chainId);
    Task ReSendFailedJobAsync(string chainId);
}

public class TransmitTransactionProvider : AbpRedisCache, ITransmitTransactionProvider, ISingletonDependency
{
    private readonly IDistributedCacheSerializer _serializer;
    private readonly IAElfClientService _aelfClientService;
    private readonly IBridgeOutService _bridgeOutService;
    private readonly INethereumService _nethereumService;
    private readonly BlockConfirmationOptions _blockConfirmationOptions;
    private readonly AElfChainAliasOptions _aelfChainAliasOption;
    private readonly EthereumChainAliasOptions _ethereumAElfChainAliasOptions;
    public ILogger<TransmitTransactionProvider> Logger { get; set; }

    private const string TransmitSendingQueue = "TransmitSendingQueue";
    private const string TransmitCheckingQueue = "TransmitCheckingQueue";
    private const string TransmitFailedQueue = "TransmitFailedQueue";

    private const int MaxSendTransmitTimes = 3;
    private const int MaxQueryTransmitTimes = 10;

    public TransmitTransactionProvider(IOptions<RedisCacheOptions> optionsAccessor,
        IOptionsSnapshot<EthereumChainAliasOptions> ethereumAElfChainAliasOptions,
        IOptionsSnapshot<AElfChainAliasOptions> aelfChainAliasOption,
        IDistributedCacheSerializer serializer, IAElfClientService aelfClientService, IBridgeOutService bridgeOutService,
        INethereumService nethereumService, IOptions<BlockConfirmationOptions> blockConfirmationOptions)
        : base(optionsAccessor)
    {
        _serializer = serializer;
        _aelfClientService = aelfClientService;
        _bridgeOutService = bridgeOutService;
        _nethereumService = nethereumService;
        _blockConfirmationOptions = blockConfirmationOptions.Value;
        _aelfChainAliasOption = aelfChainAliasOption.Value;
        _ethereumAElfChainAliasOptions = ethereumAElfChainAliasOptions.Value;
    }

    public async Task EnqueueAsync(SendTransmitArgs args)
    {
        await EnqueueAsync(GetQueueName(TransmitSendingQueue,args.ChainId), args);
    }

    public async Task SendByLibAsync(string chainId, string libHash, long libHeight)
    {
        var item = await GetFirstItemAsync(GetQueueName(TransmitSendingQueue, chainId));
        string firstSwapHashId = null;
        string firstReport = null;
        while (item != null)
        {
            if (item.BlockHeight > libHeight)
            {
                break;
            }

            if (firstSwapHashId == item.SwapHashId.ToHex() && firstReport == item.Report.ToHex())
            {
                break;
            }

            var block = await _aelfClientService.GetBlockByHeightAsync(_aelfChainAliasOption.Mapping[item.ChainId], item.BlockHeight);
            if (block.BlockHash == item.BlockHash)
            {
                if (item.SendTimes > MaxSendTransmitTimes)
                {
                    Logger.LogError($"Send transmit transaction failed after retry {item.SendTimes} times. Chain: {item.TargetChainId},  TxId: {item.TransactionId}");
                    await EnqueueAsync(GetQueueName(TransmitFailedQueue, item.ChainId), item);
                }
                else
                {
                    try
                    {
                        item.SendTimes += 1;
                        item.QueryTimes = 0;

                        var sendResult = await _bridgeOutService.TransmitAsync(item.TargetChainId,
                            item.TargetContractAddress,item.SwapHashId,
                            item.Report, item.Rs, item.Ss, item.RawVs);
                        if (string.IsNullOrWhiteSpace(sendResult))
                        {
                            Logger.LogError("Failed to transmit.");
                            await EnqueueAsync(GetQueueName(TransmitSendingQueue, item.ChainId), item);
                        }
                        else
                        {
                            item.TransactionId = sendResult;
                            await EnqueueAsync(GetQueueName(TransmitCheckingQueue,item.ChainId), item);
                            Logger.LogInformation($"Send Transmit transaction. TxId: {sendResult}, Report: {item.Report.ToHex()}"); 
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Send Transmit transaction Failed. Message: {e.Message}", e);
                        await EnqueueAsync(GetQueueName(TransmitSendingQueue, item.ChainId), item);
                    }
                    
                }
            }
            
            await DequeueAsync(GetQueueName(TransmitSendingQueue, chainId));
            if (firstSwapHashId == null && firstReport == null)
            {
                firstSwapHashId = item.SwapHashId.ToHex();
                firstReport = item.Report.ToHex();
            }
            item = await GetFirstItemAsync(GetQueueName(TransmitSendingQueue, chainId));
        }
    }

    public async Task UpdateQueueAsync(string chainId)
    {
        var item = await GetFirstItemAsync(GetQueueName(TransmitCheckingQueue, chainId));

        string firstCheckingChainId = null;
        string firstCheckingTransactionId = null;

        while (item != null)
        {
            if (item.TargetChainId == firstCheckingChainId && item.TransactionId == firstCheckingTransactionId)
            {
                break;
            }

            var ethAlias = _ethereumAElfChainAliasOptions.Mapping[item.TargetChainId];
            var receipt = await _nethereumService.GetTransactionReceiptAsync(ethAlias, item.TransactionId);

            if (receipt == null || receipt.Status == null || receipt.Status.Value != 1)
            {
                item.QueryTimes += 1;
                if (item.QueryTimes > MaxQueryTransmitTimes)
                {
                    Logger.LogDebug(
                        $"Transmit transaction query failed after retry {MaxQueryTransmitTimes} times. Chain: {item.TargetChainId},  TxId: {item.TransactionId}");
                    await EnqueueAsync(GetQueueName(TransmitSendingQueue, item.ChainId), item);
                }
                else
                {
                    Logger.LogDebug(
                        $"Transmit transaction query failed. Chain: {item.TargetChainId},  TxId: {item.TransactionId}");
                    await EnqueueAsync(GetQueueName(TransmitCheckingQueue, item.ChainId), item);
                }
            }
            else
            {
                var currentHeight = await _nethereumService.GetBlockNumberAsync(ethAlias);
                if (receipt.BlockNumber.ToLong() >=
                    currentHeight - _blockConfirmationOptions.ConfirmationCount[item.TargetChainId])
                {
                    break;
                }

                var block = await _nethereumService.GetBlockByNumberAsync(ethAlias, receipt.BlockNumber);
                if (block.BlockHash != receipt.BlockHash)
                {
                    Logger.LogError(
                        $"Transmit transaction forked. Chain: {item.TargetChainId},  TxId: {item.TransactionId}");
                    await EnqueueAsync(GetQueueName(TransmitSendingQueue, item.ChainId), item);
                }
                else
                {
                    Logger.LogInformation($"Transmit transaction finished. TxId: {item.TransactionId}");
                }
            }

            if (firstCheckingChainId == null && firstCheckingTransactionId == null)
            {
                firstCheckingChainId = item.TargetChainId;
                firstCheckingTransactionId = item.TransactionId;
            }

            await DequeueAsync(GetQueueName(TransmitCheckingQueue, chainId));
            item = await GetFirstItemAsync(GetQueueName(TransmitCheckingQueue, chainId));
        }
    }

    public async Task ReSendFailedJobAsync(string chainId)
    {
        var item = await GetFirstItemAsync(GetQueueName(TransmitFailedQueue,chainId));
        while (item != null)
        {
            item.QueryTimes = 0;
            item.SendTimes = 0;
            await EnqueueAsync(item);
            await DequeueAsync(GetQueueName(TransmitFailedQueue, chainId));
            
            item = await GetFirstItemAsync(GetQueueName(TransmitFailedQueue,chainId));
        }
    }

    private string GetQueueName(string queue, string chainId)
    {
        return $"{queue}-{chainId}";
    }

    private async Task EnqueueAsync(string queueName, SendTransmitArgs item)
    {
        await ConnectAsync();

        await RedisDatabase.ListRightPushAsync((RedisKey)queueName, _serializer.Serialize(item));
    }

    private async Task<SendTransmitArgs> DequeueAsync(string queueName)
    {
        await ConnectAsync();

        var value = await RedisDatabase.ListLeftPopAsync((RedisKey)queueName);
        return value.IsNullOrEmpty ? null : _serializer.Deserialize<SendTransmitArgs>(value);
    }
    
    private async Task<SendTransmitArgs> GetFirstItemAsync(string queueName)
    {
        await ConnectAsync();

        var value = await RedisDatabase.ListGetByIndexAsync((RedisKey)queueName,0);
        return !value.HasValue ? null : _serializer.Deserialize<SendTransmitArgs>(value);
    }
}

public class SendTransmitArgs
{
    public string ChainId { get; set; }
    public string BlockHash { get; set; }
    public long BlockHeight { get; set; }
    public string TargetChainId { get; set; }
    public string TargetContractAddress { get; set; }
    public string TransactionId { get; set; }
    public byte[] SwapHashId { get; set; }
    public byte[] Report { get; set; }
    public byte[][] Rs { get; set; }
    public byte[][] Ss{ get; set; }
    public byte[] RawVs { get; set; }
    public int QueryTimes { get; set; }
    public int SendTimes { get; set; }
}
