using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Bridge;

public interface IBridgeService
{
    Task<Hash> GetSpaceIdBySwapIdAsync(string clientAlias, Hash swapId);
}

public class BridgeService : ContractServiceBase, IBridgeService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    private readonly AElfContractOptions _contractOptions;

    private const string ContractName = "BridgeContractAddress";

    public BridgeService(IAElfClientService clientService,
        IOptionsSnapshot<AElfContractOptions> contractOptions) : base(clientService,
        Address.FromBase58(contractOptions.Value.ContractAddressList[ContractName]))
    {
        _clientService = clientService;
        _contractOptions = contractOptions.Value;
    }

    public async Task<Hash> GetSpaceIdBySwapIdAsync(string clientAlias, Hash swapId)
    {
        var result = await _clientService.ViewAsync(_contractOptions.ContractAddressList["ContractName"], "GetSpaceIdBySwapId",
            swapId, clientAlias);

        return Hash.LoadFromByteArray(result);
    }
}