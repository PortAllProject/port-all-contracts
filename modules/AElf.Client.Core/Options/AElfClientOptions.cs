namespace AElf.Client.Core.Options;

public class AElfClientOptions
{
    public List<ClientConfig> ClientConfigList { get; set; } = new();
}

public class ClientConfig
{
    public string Alias { get; set; }
    public string Endpoint { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public int Timeout { get; set; } = 60;
}