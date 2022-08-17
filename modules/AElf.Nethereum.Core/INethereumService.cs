using Nethereum.BlockchainProcessing.BlockStorage.Entities.Mapping;
using Volo.Abp.DependencyInjection;

namespace AElf.Nethereum.Core;

public interface INethereumService
{
    Task<long> GetBlockNumberAsync(string clientAlias);
}

public class NethereumService : INethereumService, ITransientDependency
{
    private readonly INethereumClientProvider _nethereumClientProvider;

    public NethereumService(INethereumClientProvider nethereumClientProvider)
    {
        _nethereumClientProvider = nethereumClientProvider;
    }

    public async Task<long> GetBlockNumberAsync(string clientAlias)
    {
        var web3 = _nethereumClientProvider.GetClient(clientAlias);
        var latestBlockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
        return latestBlockNumber.ToLong();
    }
}