using System.Collections.Concurrent;
using AElf.Nethereum.Core.Options;
using Microsoft.Extensions.Options;
using Nethereum.Web3;
using Volo.Abp.DependencyInjection;

namespace AElf.Nethereum.Core;

public interface INethereumClientProvider
{
    Web3 GetClient(string clientAlias, string accountAlias);
}

public class NethereumClientProvider : ConcurrentDictionary<NethereumClientInfo, Web3>, INethereumClientProvider, ISingletonDependency
{
    private readonly EthereumClientConfigOptions _ethereumClientConfigOptions;
    private readonly INethereumAccountProvider _nethereumAccountProvider;

    public NethereumClientProvider(IOptionsSnapshot<EthereumClientConfigOptions> ethereumClientConfigOptions,
        INethereumAccountProvider nethereumAccountProvider)
    {
        _nethereumAccountProvider = nethereumAccountProvider;
        _ethereumClientConfigOptions = ethereumClientConfigOptions.Value;
    }

    public Web3 GetClient(string clientAlias, string accountAlias)
    {
        var keys = Keys.Where(o => o.ClientAlias == clientAlias && o.AccountAlias == accountAlias).ToList();
        if (keys.Count == 0)
        {
            var account = _nethereumAccountProvider.GetAccount(accountAlias);
            var clientConfig = _ethereumClientConfigOptions.ClientConfigList
                .FirstOrDefault(o => o.Alias == clientAlias);
            var client = new Web3(account, clientConfig.Url);
            TryAdd(new NethereumClientInfo
            {
                ClientAlias = clientAlias,
                AccountAlias = accountAlias
            }, client);

            return client;
        }

        return this[keys.Single()];
    }
}

public class NethereumClientInfo
{
    public string ClientAlias { get; set; }
    public string AccountAlias { get; set; }
}
