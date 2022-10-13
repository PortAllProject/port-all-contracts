using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Contracts.Oracle;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Oracle;

public interface IOracleService
{
    Task<SendTransactionResult> QueryAsync(string chainId, QueryInput queryInput);

    Task<SendTransactionResult> CommitAsync(string chainId, CommitInput commitInput);

    Task<SendTransactionResult> RevealAsync(string chainId, RevealInput revealInput);

    Task<SendTransactionResult> CancelQueryAsync(string chainId, Hash cancelQueryInput);
}

public class OracleService : ContractServiceBase, IOracleService, ITransientDependency
{
    protected override string SmartContractName { get; } = "OracleContract";

    public async Task<SendTransactionResult> QueryAsync(string chainId, QueryInput queryInput)
    {
        var tx = await PerformSendTransactionAsync("Query", queryInput, chainId);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), chainId)
        };
    }

    public async Task<SendTransactionResult> CommitAsync(string chainId, CommitInput commitInput)
    {
        var tx = await PerformSendTransactionAsync("Commit", commitInput, chainId);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), chainId)
        };
    }

    public async Task<SendTransactionResult> RevealAsync(string chainId, RevealInput revealInput)
    {
        var tx = await PerformSendTransactionAsync("Reveal", revealInput, chainId);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), chainId)
        };
    }

    public async Task<SendTransactionResult> CancelQueryAsync(string chainId, Hash cancelQueryInput)
    {
        var tx = await PerformSendTransactionAsync("CancelQuery", cancelQueryInput, chainId);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), chainId)
        };
    }
    
}