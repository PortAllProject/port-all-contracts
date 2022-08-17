namespace AElf.Nethereum.Core.Options;

public class EthereumClientConfigOptions
{
    public List<EthereumClient> ClientConfigList { get; set; } = new();
}

public class EthereumClient
{
    public string Alias { get; set; }
    
    public string Url { get; set; }
}