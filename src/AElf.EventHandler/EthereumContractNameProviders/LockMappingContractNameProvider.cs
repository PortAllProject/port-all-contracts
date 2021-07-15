namespace AElf.EventHandler
{
    public class LockMappingContractNameProvider : IEthereumContractNameProvider
    {
        public string ContractName => "LockMapping";
        public string AbiFileName => "LockAbi";
        public string AddressConfigName => "LockMappingContractAddress";
    }
}