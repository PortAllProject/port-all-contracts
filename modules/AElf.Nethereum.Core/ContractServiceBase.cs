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
    public EthereumClientAccountMappingOptions EthereumClientAccountMappingOptions { get; set; }
    public EthereumContractOptions EthereumContractOptions { get; set; }
    protected abstract string SmartContractName { get; }

    protected Function GetFunction(string clientAlias, string methodName)
    {
        var accountAlias = EthereumClientAccountMappingOptions.Mapping[clientAlias];
        var client = NethereumClientProvider.GetClient(clientAlias, accountAlias);
        var contractAddress = EthereumContractOptions.ContractInfoList[SmartContractName].ContractAddress;
        var contract = client.Eth.GetContract(GetAbi(), contractAddress);
        return contract.GetFunction(methodName);
    }

    protected Account GetAccount(string clientAlias)
    {
        var accountAlias = EthereumClientAccountMappingOptions.Mapping[clientAlias];
        return NethereumAccountProvider.GetAccount(accountAlias);
    }

    private string GetAbi()
    {
        var path = Path.Combine(EthereumContractOptions.AbiFileDirectory,
            EthereumContractOptions.ContractInfoList[SmartContractName].AbiFileName);
        
        using var file = System.IO.File.OpenText(path);
        using var reader = new JsonTextReader(file);
        var o = (JObject) JToken.ReadFrom(reader);
        var value = o["abi"]?.ToString();
        return value;
    }
}