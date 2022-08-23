namespace AElf.Client.Core.Options;

public class AElfContractOptions
{
    public string ContractDirectory { get; set; }
    public Dictionary<string, Dictionary<string, string>> ContractAddressList { get; set; } = new();
}