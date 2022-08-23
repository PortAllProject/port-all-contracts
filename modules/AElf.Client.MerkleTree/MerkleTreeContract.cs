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
    Task<SendTransactionResult> CreateSpaceAsync(string clientAlias, CreateSpaceInput createSpaceInput);

    Task<Int64Value> GetLastLeafIndexAsync(string clientAlias, GetLastLeafIndexInput getLastLeafIndexInput);
}

public class MerkleTreeContractService : ContractServiceBase, IMerkleTreeContractService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    private readonly AElfContractOptions _contractOptions;
    
    protected override string SmartContractName { get; } = "MerkleTreeContract";

    public MerkleTreeContractService(IAElfClientService clientService,
        IOptionsSnapshot<AElfContractOptions> contractOptions) 
    {
        _clientService = clientService;
        _contractOptions = contractOptions.Value;
    }

    public async Task<SendTransactionResult> CreateSpaceAsync(string clientAlias, CreateSpaceInput createSpaceInput)
    {
        var tx = await PerformSendTransactionAsync("CreateSpace", createSpaceInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<Int64Value> GetLastLeafIndexAsync(string clientAlias, GetLastLeafIndexInput getLastLeafIndexInput)
    {
        var result = await _clientService.ViewAsync(GetContractAddress(clientAlias), "GetLastLeafIndex",
            getLastLeafIndexInput, clientAlias);
        var actualResult = new Int64Value();
        actualResult.MergeFrom(result);
        return actualResult;
    }
}