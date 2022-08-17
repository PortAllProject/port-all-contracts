using System.Collections.Concurrent;
using AElf.Nethereum.Core.Options;
using Microsoft.Extensions.Options;
using Nethereum.Web3.Accounts;
using Volo.Abp.DependencyInjection;

namespace AElf.Nethereum.Core;

public interface INethereumAccountProvider
{
    Account GetAccount(string alias);
}

public class NethereumAccountProvider : ConcurrentDictionary<string, Account>, INethereumAccountProvider, ISingletonDependency
{
    private readonly EthereumAccountOptions _ethereumAccountOptions;

    public NethereumAccountProvider(IOptionsSnapshot<EthereumAccountOptions> optionsSnapshot)
    {
        _ethereumAccountOptions = optionsSnapshot.Value;

        foreach (var item in _ethereumAccountOptions.AccountConfigList)
        {
            TryAdd(item.Alias, new Account(item.PrivateKey));
        }
    }

    public Account GetAccount(string alias)
    {
        return this[alias];
    }
}