using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Volo.Abp.Threading;

namespace AElf.Client.Core;

public class ContractServiceBase
{
    private readonly IAElfClientService _clientService;
    protected string SmartContractName { get; }
    protected Address? ContractAddress { get; set; }

    public ILogger<ContractServiceBase> Logger { get; set; }

    protected ContractServiceBase(IAElfClientService clientService, string smartContractName)
    {
        _clientService = clientService;
        SmartContractName = smartContractName;
    }

    protected ContractServiceBase(IAElfClientService clientService, Address contractAddress)
    {
        _clientService = clientService;
        ContractAddress = contractAddress;
    }

    protected async Task<Transaction> PerformSendTransactionAsync(string methodName, IMessage parameter,
        string useClientAlias, string? smartContractName = null)
    {
        if (smartContractName == null)
        {
            smartContractName = SmartContractName;
        }

        if (ContractAddress != null)
        {
            return await _clientService.SendAsync(ContractAddress.ToBase58(), methodName, parameter, useClientAlias);
        }

        return await _clientService.SendSystemAsync(smartContractName, methodName, parameter, useClientAlias);
    }

    protected async Task<TransactionResult> PerformGetTransactionResultAsync(string transactionId,
        string useClientAlias)
    {
        TransactionResult txResult;
        do
        {
            txResult = await _clientService.GetTransactionResultAsync(transactionId, useClientAlias);
        } while (txResult.Status == TransactionResultStatus.Pending);

        Logger.LogInformation("{TxResult}", txResult);
        return txResult;
    }
}