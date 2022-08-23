using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Contracts.Bridge;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Bridge;

public interface IBridgeService
{
    Task<Hash> GetSpaceIdBySwapIdAsync(string clientAlias, Hash swapId);
    Task<SendTransactionResult> SetGasPriceAsync(string clientAlias, SetGasPriceInput input);
    Task<SendTransactionResult> SetPriceRatioAsync(string clientAlias, SetPriceRatioInput input);
}

public class BridgeService : ContractServiceBase, IBridgeService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    private readonly AElfContractOptions _contractOptions;

    protected override string SmartContractName { get; }= "BridgeContractAddress";

    public BridgeService(IAElfClientService clientService,
        IOptionsSnapshot<AElfContractOptions> contractOptions)
    {
        _clientService = clientService;
        _contractOptions = contractOptions.Value;
    }

    public async Task<Hash> GetSpaceIdBySwapIdAsync(string clientAlias, Hash swapId)
    {
        var result = await _clientService.ViewAsync(GetContractAddress(clientAlias), "GetSpaceIdBySwapId",
            swapId, clientAlias);

        return Hash.LoadFromByteArray(result);
    }

    public async Task<SendTransactionResult> SetGasPriceAsync(string clientAlias, SetGasPriceInput input)
    {
        var tx = await PerformSendTransactionAsync("SetGasPrice", input, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }
    
    public async Task<SendTransactionResult> SetPriceRatioAsync(string clientAlias, SetPriceRatioInput input)
    {
        var tx = await PerformSendTransactionAsync("SetPriceRatio", input, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    
}