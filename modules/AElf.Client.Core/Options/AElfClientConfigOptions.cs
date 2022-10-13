namespace AElf.Client.Core.Options;

public class AElfClientConfigOptions
{
    public string ClientAlias { get; set; } = "TestNetSideChain2";
    public string MainChainClientAlias { get; set; } = "TestNetMainChain";
    public string SideChainClientAlias { get; set; } = "TestNetSideChain2";
    public string AccountAlias { get; set; } = "Default";
    public bool CamelCase { get; set; } = false;
}