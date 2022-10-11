namespace AElf.Client.Core.Options;

public class AElfAccountOptions
{
    public string KeyDirectory { get; set; }
    public List<AccountConfig> AccountConfigList { get; set; } = new();
}

public class AccountConfig
{
    public string Alias { get; set; }
    public string Address { get; set; }
    public string Password { get; set; }
    public string PrivateKey { get; set; }
}