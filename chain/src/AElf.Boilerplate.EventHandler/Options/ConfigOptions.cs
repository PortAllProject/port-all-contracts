using System.Collections.Generic;

namespace AElf.Boilerplate.EventHandler
{
    public class ConfigOptions
    {
        public string BlockChainEndpoint { get; set; }
        public string AccountAddress { get; set; }
        public string AccountPassword { get; set; }
        public string EthereumContractAddress { get; set; }
        public List<string> ObserverAssociationAddressList { get; set; }
    }
}