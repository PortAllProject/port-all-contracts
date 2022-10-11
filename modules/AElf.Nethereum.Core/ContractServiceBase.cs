using AElf.Nethereum.Core.Options;
using Microsoft.Extensions.Options;
using Nethereum.Contracts;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AElf.Nethereum.Core;

public abstract class ContractServiceBase
{
    public INethereumClientProvider NethereumClientProvider { get; set; }
    public INethereumAccountProvider NethereumAccountProvider { get; set; }
    public IOptionsSnapshot<EthereumContractOptions> EthereumContractOptions { get; set; }
    public IOptionsSnapshot<EthereumClientConfigOptions> EthereumClientConfigOptions { get; set; }
    public IOptionsSnapshot<EthereumChainAliasOptions> EthereumAElfChainAliasOptions { get; set; }
    protected abstract string SmartContractName { get; }

    protected Function GetFunction(string chainId, string contractAddress, string methodName)
    {
        var clientAlias = EthereumAElfChainAliasOptions.Value.Mapping[chainId];
        var accountAlias = EthereumClientConfigOptions.Value.AccountAlias;
        var client = NethereumClientProvider.GetClient(clientAlias, accountAlias);
        var contract = client.Eth.GetContract(GetAbi(), contractAddress);
        return contract.GetFunction(methodName);
    }

    protected Account GetAccount()
    {
        return NethereumAccountProvider.GetAccount(EthereumClientConfigOptions.Value.AccountAlias);
    }

    private string GetAbi()
    {
        var path = Path.Combine(EthereumContractOptions.Value.AbiFileDirectory,
            EthereumContractOptions.Value.ContractInfoList[SmartContractName].AbiFileName);
        
        using var file = System.IO.File.OpenText(path);
        using var reader = new JsonTextReader(file);
        var o = (JObject) JToken.ReadFrom(reader);
        var value = o["abi"]?.ToString();
        return value;
    }
}