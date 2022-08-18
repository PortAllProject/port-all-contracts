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
    Task<SendTransactionResult> CreateSpaceAsync(CreateSpaceInput createSpaceInput);

    Task<Int64Value> GetLastLeafIndexAsync(GetLastLeafIndexInput getLastLeafIndexInput);
}

public class MerkleTreeContractService : ContractServiceBase, IMerkleTreeContractService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    private readonly AElfClientConfigOptions _clientConfigOptions;
    private readonly AElfContractOptions _contractOptions;

    public MerkleTreeContractService(IAElfClientService clientService,
        IOptionsSnapshot<AElfClientConfigOptions> clientConfigOptions,
        IOptionsSnapshot<AElfContractOptions> contractOptions) : base(clientService,
        Address.FromBase58(contractOptions.Value.ContractAddressList[""]))
    {
        _clientService = clientService;
        _clientConfigOptions = clientConfigOptions.Value;
        _contractOptions = contractOptions.Value;
    }

    public async Task<SendTransactionResult> CreateSpaceAsync(CreateSpaceInput createSpaceInput)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var tx = await PerformSendTransactionAsync("CreateSpace", createSpaceInput, useClientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), useClientAlias)
        };
    }

    public async Task<Int64Value> GetLastLeafIndexAsync(GetLastLeafIndexInput getLastLeafIndexInput)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;
        var result = await _clientService.ViewAsync(_contractOptions.ContractAddressList[""], "GetLastLeafIndex",
            getLastLeafIndexInput, useClientAlias);
        var actualResult = new Int64Value();
        actualResult.MergeFrom(result);
        return actualResult;
    }
}