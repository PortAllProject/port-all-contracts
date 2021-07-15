namespace AElf.EventHandler
{
    public class TransmitContractNameProvider : IEthereumContractNameProvider
    {
        public string ContractName => "TransmitContract";
        public string AbiFileName => "TransmitAbi";
        public string AddressConfigName => "TransmitContractAddress";
    }
}