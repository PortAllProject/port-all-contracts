using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Contracts.Oracle;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Oracle;

public interface IOracleService
{
    Task<SendTransactionResult> QueryAsync(string clientAlias, QueryInput queryInput);

    Task<SendTransactionResult> CommitAsync(string clientAlias, CommitInput commitInput);

    Task<SendTransactionResult> RevealAsync(string clientAlias, RevealInput revealInput);

    Task<SendTransactionResult> CancelQueryAsync(string clientAlias, Hash cancelQueryInput);
}

public class OracleService : ContractServiceBase, IOracleService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    private readonly AElfClientConfigOptions _clientConfigOptions;

    protected override string SmartContractName { get; } = "OracleContract";

    public OracleService(IAElfClientService clientService,
        IOptionsSnapshot<AElfClientConfigOptions> clientConfigOptions,
        IOptionsSnapshot<AElfContractOptions> contractOptions) 
    {
        _clientService = clientService;
        _clientConfigOptions = clientConfigOptions.Value;
    }

    public async Task<SendTransactionResult> QueryAsync(string clientAlias, QueryInput queryInput)
    {
        var tx = await PerformSendTransactionAsync("Query", queryInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<SendTransactionResult> CommitAsync(string clientAlias, CommitInput commitInput)
    {
        var tx = await PerformSendTransactionAsync("Commit", commitInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<SendTransactionResult> RevealAsync(string clientAlias, RevealInput revealInput)
    {
        var tx = await PerformSendTransactionAsync("Reveal", revealInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<SendTransactionResult> CancelQueryAsync(string clientAlias, Hash cancelQueryInput)
    {
        var tx = await PerformSendTransactionAsync("CancelQuery", cancelQueryInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }
    
}