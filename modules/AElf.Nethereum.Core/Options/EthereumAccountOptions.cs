namespace AElf.Nethereum.Core.Options;

public class EthereumAccountOptions
{
    public string KeyDirectory { get; set; }
    public List<EthereumAccountConfig> AccountConfigList { get; set; } = new();
}

public class EthereumAccountConfig
{
    public string Alias { get; set; }
    public string Address { get; set; }
    public string Password { get; set; }
    public string PrivateKey { get; set; }
}