using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Nethereum.Bridge;
using AElf.Nethereum.Core;
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
}

public class TransmitTransactionProvider : AbpRedisCache, ITransmitTransactionProvider, ISingletonDependency
{
    private readonly IDistributedCacheSerializer _serializer;
    private readonly IAElfClientService _aelfClientService;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;
    private readonly IBridgeOutService _bridgeOutService;
    private readonly INethereumService _nethereumService;
    private readonly BlockConfirmationOptions _blockConfirmationOptions;

    public ILogger<TransmitTransactionProvider> Logger { get; set; }

    private const string TransmitSendingQueue = "TransmitSendingQueue";
    private const string TransmitCheckingQueue = "TransmitCheckingQueue";
    private const string TransmitFailedQueue = "TransmitFailedQueue";

    public TransmitTransactionProvider(IOptions<RedisCacheOptions> optionsAccessor,
        IOptions<AElfClientConfigOptions> aelfClientConfigOptions,
        IDistributedCacheSerializer serializer, IAElfClientService aelfClientService, IBridgeOutService bridgeOutService,
        INethereumService nethereumService, IOptions<BlockConfirmationOptions> blockConfirmationOptions)
        : base(optionsAccessor)
    {
        _serializer = serializer;
        _aelfClientService = aelfClientService;
        _bridgeOutService = bridgeOutService;
        _nethereumService = nethereumService;
        _aelfClientConfigOptions = aelfClientConfigOptions.Value;
        _blockConfirmationOptions = blockConfirmationOptions.Value;
    }

    public async Task EnqueueAsync(SendTransmitArgs args)
    {
        await EnqueueAsync(GetQueueName(TransmitSendingQueue,args.ChainId), args);
    }

    public async Task SendByLibAsync(string chainId, string libHash, long libHeight)
    {
        var item = await GetFirstItemAsync(GetQueueName(TransmitSendingQueue, chainId));
        while (item != null)
        {
            if (item.BlockHeight > libHeight)
            {
                break;
            }

            var block = await _aelfClientService.GetBlockByHeightAsync(_aelfClientConfigOptions.ClientAlias, item.BlockHeight);
            if (block.BlockHash == item.BlockHash)
            {
                if (item.RetryTimes > 3)
                {
                    Logger.LogError($"Transmit transaction failed after retry {item.RetryTimes-1} times. Chain: {item.TargetChainId},  TxId: {item.TransactionId}");
                    await EnqueueAsync(GetQueueName(TransmitFailedQueue, item.ChainId), item);
                }
                else
                {
                    var sendResult = await _bridgeOutService.TransmitAsync(item.TargetChainId,
                        item.TargetContractAddress,item.SwapHashId,
                        item.Report, item.Rs, item.Ss, item.RawVs);
                    if (string.IsNullOrWhiteSpace(sendResult))
                    {
                        Logger.LogError("Failed to transmit.");
                        break;
                    }

                    item.TransactionId = sendResult;
                    await EnqueueAsync(GetQueueName(TransmitCheckingQueue,item.ChainId), item);
                }
            }
            
            await DequeueAsync(GetQueueName(TransmitSendingQueue, chainId));
            item = await GetFirstItemAsync(GetQueueName(TransmitSendingQueue, chainId));
        }
    }

    public async Task UpdateQueueAsync(string chainId)
    {
        var item = await GetFirstItemAsync(GetQueueName(TransmitCheckingQueue,chainId));
        while (item != null)
        {
            var receipt = await _nethereumService.GetTransactionReceiptAsync(item.TargetChainId, item.TransactionId);

            if (receipt.Status.Value != 1)
            {
                Logger.LogError($"Transmit transaction failed. Chain: {item.TargetChainId},  TxId: {item.TransactionId}");
                item.RetryTimes += 1;
                await EnqueueAsync(GetQueueName(TransmitSendingQueue, item.ChainId), item);
            }

            var currentHeight = await _nethereumService.GetBlockNumberAsync(item.TargetChainId);
            if (receipt.BlockNumber.ToLong() >= currentHeight - _blockConfirmationOptions.ConfirmationCount[item.TargetChainId])
            {
                break;
            }

            var block = await _nethereumService.GetBlockByNumberAsync(item.TargetChainId, receipt.BlockNumber);
            if (block.BlockHash != receipt.BlockHash)
            {
                Logger.LogError($"Transmit transaction forked. Chain: {item.TargetChainId},  TxId: {item.TransactionId}");
                item.RetryTimes += 1;
                await EnqueueAsync(GetQueueName(TransmitSendingQueue, item.ChainId), item);
            }
            
            await DequeueAsync(GetQueueName(TransmitCheckingQueue,chainId));
            item = await GetFirstItemAsync(GetQueueName(TransmitCheckingQueue,chainId));
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
    public int RetryTimes { get; set; }
    
}
