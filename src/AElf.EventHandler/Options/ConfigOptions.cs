using System.Collections.Generic;

namespace AElf.EventHandler
{
    public class ConfigOptions
    {
        public string BlockChainEndpoint { get; set; }
        public string AccountAddress { get; set; }
        public string AccountPassword { get; set; }
        public string Token { get; set; }
        public List<string> ObserverAssociationAddressList { get; set; }
        public string TransmitContractAddress { get; set; }
        public string MerkleGeneratorContractAddress { get; set; }
        public List<SwapConfig> SwapConfigs { get; set; }
        public int MaximumLeafCount { get; set; }
        public bool SendQueryTransaction { get; set; }
        public long QueryPayment { get; set; } = 1_0000_0000;
        public string TokenSwapOracleOrganizationAddress { get; set; }
    }

    public class SwapConfig
    {
        public string TokenSymbol { get; set; }
        public long RecorderId { get; set; }
        public string LockMappingContractAddress { get; set; }
        public string NodeUrl { get; set; }
        public bool CanTakeToken { get; set; }
    }
}