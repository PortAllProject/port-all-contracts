using AElf.Client.Core.Options;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Core;

public interface IAElfClientProvider
{
    AElfClient GetClient(string? alias = null, string? environment = null, int? chainId = null, string? chainType = null);

    void SetClient(AElfClient client, string? environment = null, int? chainId = null, string? chainType = null,
        string? alias = null);
}

public class AElfClientProvider : Dictionary<AElfClientInfo, AElfClient>, IAElfClientProvider, ISingletonDependency
{
    public AElfClientProvider(IOptionsSnapshot<AElfClientOptions> aelfClientOptions,
        IOptionsSnapshot<AElfClientConfigOptions> aelfClientConfigOptions)
    {
        var useCamelCase = aelfClientConfigOptions.Value.CamelCase;
        var clientBuilder = new AElfClientBuilder();
        SetClient(clientBuilder.UsePublicEndpoint(EndpointType.MainNetMainChain).UseCamelCase(useCamelCase).Build(),
            "MainNet", AElfClientConstants.MainChainId, "MainChain", EndpointType.MainNetMainChain.ToString());
        SetClient(clientBuilder.UsePublicEndpoint(EndpointType.MainNetSideChain1).UseCamelCase(useCamelCase).Build(),
            "MainNet", AElfClientConstants.SideChainId2, "SideChain", EndpointType.MainNetSideChain1.ToString());
        SetClient(clientBuilder.UsePublicEndpoint(EndpointType.TestNetMainChain).UseCamelCase(useCamelCase).Build(),
            "TestNet", AElfClientConstants.MainChainId, "MainChain", EndpointType.TestNetMainChain.ToString());
        SetClient(clientBuilder.UsePublicEndpoint(EndpointType.TestNetSideChain2).UseCamelCase(useCamelCase).Build(),
            "MainNet", AElfClientConstants.SideChainId2, "SideChain", EndpointType.TestNetSideChain2.ToString());
        SetClient(clientBuilder.UsePublicEndpoint(EndpointType.Local).UseCamelCase(useCamelCase).Build(), "Local",
            AElfClientConstants.MainChainId, "MainChain", EndpointType.Local.ToString());

        foreach (var clientConfig in aelfClientOptions.Value.ClientConfigList)
        {
            var client = clientBuilder
                .UseEndpoint(clientConfig.Endpoint)
                .ManagePeerInfo(clientConfig.UserName, clientConfig.Password)
                .SetHttpTimeout(clientConfig.Timeout)
                .Build();
            SetClient(client, alias: clientConfig.Alias);
        }
    }

    public AElfClient GetClient(string? alias = null, string? environment = null, int? chainId = null,
        string? chainType = null)
    {
        var keys = Keys
            .WhereIf(!alias.IsNullOrWhiteSpace(), c => c.Alias == alias)
            .WhereIf(!environment.IsNullOrWhiteSpace(), c => c.Environment == environment)
            .WhereIf(chainId.HasValue, c => c.ChainId == chainId)
            .WhereIf(!chainType.IsNullOrWhiteSpace(), c => c.ChainType == chainType)
            .ToList();
        if (keys.Count != 1)
        {
            throw new AElfClientException(
                $"Failed to get client of {alias} - {environment} - {chainId} - {chainType}.");
        }

        return this[keys.Single()];
    }

    public void SetClient(AElfClient client, string? environment = null, int? chainId = null, string? chainType = null,
        string? alias = null)
    {
        TryAdd(new AElfClientInfo
        {
            Environment = environment,
            ChainId = chainId,
            ChainType = chainType,
            Alias = alias
        }, client);
    }
}

public class AElfClientInfo
{
    public string? Environment { get; set; }
    public int? ChainId { get; set; }
    public string? ChainType { get; set; }
    public string? Alias { get; set; }
}