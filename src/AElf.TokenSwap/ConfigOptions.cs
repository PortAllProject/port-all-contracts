using System.Collections.Generic;
using AElf.Contracts.Bridge;
using AElf.Types;

namespace AElf.TokenSwap
{
    public class ConfigOptions
    {
        public string AccountAddress { get; set; }
        public string BlockChainEndpoint { get; set; }
        public string BridgeContractAddress { get; set; }
        public string ElectionContractAddress { get; set; }
        public string EthereumPrivateKey { get; set; }
        public string LockAbiFilePath { get; set; }
        public List<SwapInformation> SwapList { get; set; }
    }

    public class SwapInformation
    {
        public long RecorderId { get; set; }
        public string SwapId { get; set; }
        public List<string> TokenSymbols { get; set; }
        public int Decimal { get; set; }
        public string LockMappingContractAddress { get; set; }
        public string NodeUrl { get; set; }
    }
}