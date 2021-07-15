namespace AElf.EventHandler
{
    public interface IEthereumContractNameProvider
    {
        string ContractName { get; }
        string AbiFileName { get; }
        string AddressConfigName { get; }
    }
}