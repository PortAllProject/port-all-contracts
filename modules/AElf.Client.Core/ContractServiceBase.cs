using AElf.Client.Core.Options;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Client.Core;

public abstract class ContractServiceBase
{
    public IAElfClientService ClientService { get; set; }
    public IOptionsSnapshot<AElfContractOptions> ContractOptions { get; set; }
    protected abstract string SmartContractName { get; }
    public ILogger<ContractServiceBase> Logger { get; set; }
    public IOptionsSnapshot<AElfChainAliasOptions> AElfChainAliasOptions { get; set; }

    protected async Task<Transaction> PerformSendTransactionAsync(string methodName, IMessage parameter,
        string chainId)
    {
        var contractAddress = GetContractAddress(chainId);
        var clientAlias = AElfChainAliasOptions.Value.Mapping[chainId];
        return await ClientService.SendAsync(contractAddress, methodName, parameter, clientAlias);
    }

    protected async Task<TransactionResult> PerformGetTransactionResultAsync(string transactionId,
        string chainId)
    {
        TransactionResult txResult;
        var clientAlias = AElfChainAliasOptions.Value.Mapping[chainId];
        do
        {
            txResult = await ClientService.GetTransactionResultAsync(transactionId, clientAlias);
        } while (txResult.Status == TransactionResultStatus.Pending);

        Logger.LogInformation("{TxResult}", txResult);
        return txResult;
    }

    protected string GetContractAddress(string chainId)
    {
        return ContractOptions.Value.ContractAddressList[chainId][SmartContractName];
    }
}