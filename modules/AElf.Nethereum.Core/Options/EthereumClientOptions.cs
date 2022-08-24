namespace AElf.Nethereum.Core.Options;

public class EthereumClientOptions
{
    public List<EthereumClient> ClientConfigList { get; set; }
}

public class EthereumClient
{
    public string Alias { get; set; }
    public string Url { get; set; }
}