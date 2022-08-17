using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Bridge;

public interface IBridgeService
{
    Task<Hash> GetSpaceIdBySwapIdAsync(Hash swapId);
}

public class BridgeService : ContractServiceBase, IBridgeService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    private readonly AElfClientConfigOptions _clientConfigOptions;
    private readonly AElfContractOptions _contractOptions;

    private const string ContractName = "BridgeContractAddress";

    protected BridgeService(IAElfClientService clientService,
        IOptionsSnapshot<AElfClientConfigOptions> clientConfigOptions,
        IOptionsSnapshot<AElfContractOptions> contractOptions) : base(clientService,
        Address.FromBase58(contractOptions.Value.ContractAddressList[ContractName]))
    {
        _clientService = clientService;
        _clientConfigOptions = clientConfigOptions.Value;
        _contractOptions = contractOptions.Value;
    }

    public async Task<Hash> GetSpaceIdBySwapIdAsync(Hash swapId)
    {
        var useClientAlias = _clientConfigOptions.ClientAlias;

        var result = await _clientService.ViewAsync(_contractOptions.ContractAddressList["ContractName"], "GetSpaceIdBySwapId",
            swapId, useClientAlias);

        return Hash.LoadFromByteArray(result);
    }
}