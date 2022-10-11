namespace AElf.Nethereum.Core.Options;

public class EthereumContractOptions
{
    public string AbiFileDirectory { get; set; } = "ContractBuild";
    public Dictionary<string, EthereumContractInfo> ContractInfoList { get; set; }
}

public class EthereumContractInfo
{
    public string AbiFileName { get; set; }
}