using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Contracts.Bridge;
using AElf.Contracts.Report;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Bridge;

public interface IBridgeService
{
    Task<Hash> GetSpaceIdBySwapIdAsync(string chainId, Hash swapId);
    Task<SendTransactionResult> SetGasPriceAsync(string chainId, SetGasPriceInput input);
    Task<SendTransactionResult> SetPriceRatioAsync(string chainId, SetPriceRatioInput input);
    Task<StringValue> GetGasPriceAsync(string chainId, StringValue input);
    Task<StringValue> GetPriceRatioAsync(string chainId, StringValue input);
    Task<ReceiptIdInfo> GetReceiptIdInfoAsync(string chainId, Hash receiptIdHash);
}

public class BridgeService : ContractServiceBase, IBridgeService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    private readonly AElfContractOptions _contractOptions;

    protected override string SmartContractName { get; }= "BridgeContract";

    public BridgeService(IAElfClientService clientService,
        IOptionsSnapshot<AElfContractOptions> contractOptions)
    {
        _clientService = clientService;
        _contractOptions = contractOptions.Value;
    }

    public async Task<Hash> GetSpaceIdBySwapIdAsync(string chainId, Hash swapId)
    {
        var result = await _clientService.ViewAsync(GetContractAddress(chainId), "GetSpaceIdBySwapId",
            swapId, chainId);

        return Hash.LoadFromByteArray(result);
    }

    public async Task<SendTransactionResult> SetGasPriceAsync(string chainId, SetGasPriceInput input)
    {
        var tx = await PerformSendTransactionAsync("SetGasPrice", input, chainId);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), chainId)
        };
    }
    
    public async Task<SendTransactionResult> SetPriceRatioAsync(string chainId, SetPriceRatioInput input)
    {
        var tx = await PerformSendTransactionAsync("SetPriceRatio", input, chainId);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), chainId)
        };
    }

    public async Task<StringValue> GetGasPriceAsync(string chainId, StringValue input)
    {
        var result = await _clientService.ViewAsync(GetContractAddress(chainId), "GetGasPrice",
            input, AElfChainAliasOptions.Value.Mapping[chainId]);
        var actualResult = new StringValue();
        actualResult.MergeFrom(result);
        return actualResult;
    }

    public async Task<StringValue> GetPriceRatioAsync(string chainId, StringValue input)
    {
        var result = await _clientService.ViewAsync(GetContractAddress(chainId), "GetPriceRatio",
            input, AElfChainAliasOptions.Value.Mapping[chainId]);
        var actualResult = new StringValue();
        actualResult.MergeFrom(result);
        return actualResult;
    }

    public async Task<ReceiptIdInfo> GetReceiptIdInfoAsync(string chainId, Hash receiptIdHash)
    {
        var result = await _clientService.ViewAsync(GetContractAddress(chainId), "GetReceiptIdInfo",
            receiptIdHash, AElfChainAliasOptions.Value.Mapping[chainId]);
        var actualResult = new ReceiptIdInfo();
        actualResult.MergeFrom(result);
        return actualResult;
    }
}