using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Contracts.MerkleTreeContract;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Google.Protobuf.WellKnownTypes;
using Google.Protobuf;

namespace AElf.Client.MerkleTreeContract;

public interface IMerkleTreeContractService
{
    Task<SendTransactionResult> CreateSpaceAsync(string chainId, CreateSpaceInput createSpaceInput);

    Task<Int64Value> GetLastLeafIndexAsync(string chainId, GetLastLeafIndexInput getLastLeafIndexInput);
}

public class MerkleTreeContractService : ContractServiceBase, IMerkleTreeContractService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    
    protected override string SmartContractName { get; } = "MerkleTreeContract";

    public MerkleTreeContractService(IAElfClientService clientService) 
    {
        _clientService = clientService;
    }

    public async Task<SendTransactionResult> CreateSpaceAsync(string chainId, CreateSpaceInput createSpaceInput)
    {
        var tx = await PerformSendTransactionAsync("CreateSpace", createSpaceInput, chainId);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), chainId)
        };
    }

    public async Task<Int64Value> GetLastLeafIndexAsync(string chainId, GetLastLeafIndexInput getLastLeafIndexInput)
    {
        var clientAlias = AElfChainAliasOptions.Value.Mapping[chainId];
        var result = await _clientService.ViewAsync(GetContractAddress(chainId), "GetLastLeafIndex",
            getLastLeafIndexInput, clientAlias);
        var actualResult = new Int64Value();
        actualResult.MergeFrom(result);
        return actualResult;
    }
}