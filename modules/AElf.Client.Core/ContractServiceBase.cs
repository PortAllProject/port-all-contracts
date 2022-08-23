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

    protected async Task<Transaction> PerformSendTransactionAsync(string methodName, IMessage parameter,
        string useClientAlias)
    {
        var contractAddress = GetContractAddress(useClientAlias);
        return await ClientService.SendAsync(contractAddress, methodName, parameter, useClientAlias);
    }

    protected async Task<TransactionResult> PerformGetTransactionResultAsync(string transactionId,
        string useClientAlias)
    {
        TransactionResult txResult;
        do
        {
            txResult = await ClientService.GetTransactionResultAsync(transactionId, useClientAlias);
        } while (txResult.Status == TransactionResultStatus.Pending);

        Logger.LogInformation("{TxResult}", txResult);
        return txResult;
    }

    protected string GetContractAddress(string chainId)
    {
        return ContractOptions.Value.ContractAddressList[chainId][SmartContractName];
    }
}