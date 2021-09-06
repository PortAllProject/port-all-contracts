namespace AElf.TokenSwap
{
    public class ConfigOptions
    {
        public string AccountAddress { get; set; }
        public string BlockChainEndpoint { get; set; }
        public string BridgeContractAddress { get; set; }
        public string TokenContractAddress { get; set; }
        public string LotteryContractAddress { get; set; }
        public string SwapId { get; set; }
        public string LockMappingContractAddress { get; set; }
        public string EthereumUrl { get; set; }
        public string EthereumPrivateKey { get; set; }
        public string LockAbiFilePath { get; set; }
    }
}