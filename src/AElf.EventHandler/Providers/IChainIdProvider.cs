using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler;

public interface IChainIdProvider
{
    string GetChainId(int chainId);
}

public class ChainIdProvider : IChainIdProvider, ITransientDependency
{
    private readonly ChainIdMappingOptions _chainIdMappingOptions;

    public ChainIdProvider(IOptionsSnapshot<ChainIdMappingOptions> chainIdMappingOptions)
    {
        _chainIdMappingOptions = chainIdMappingOptions.Value;
    }

    public string GetChainId(int chainId)
    {
        return _chainIdMappingOptions.Mapping[chainId.ToString()];
    }
}