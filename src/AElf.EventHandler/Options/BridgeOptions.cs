using System.Collections.Generic;

namespace AElf.EventHandler;

public class BridgeOptions
{
    public bool IsSendQuery { get; set; }
    public bool IsTransmitter { get; set; }
    public long QueryPayment { get; set; }
    
    public List<BridgeItemIn> BridgesIn { get; set; }

    public List<BridgeItemOut> BridgesOut { get; set; }
    public string AccountAddress { get; set; }
}

//Others -> AElf
public class BridgeItemIn
{
    public string ChainId { get; set; }
    public string TargetChainId { get; set; }
    public string EthereumBridgeInContractAddress { get; set; }
    public string OriginToken { get; set; }
    public string QueryToAddress { get; set; }
    public string SwapId { get; set; }
    public string MaximumLeafCount { get; set; }
}


//AElf to others
public class BridgeItemOut
{
    public string ChainId { get; set; }
    public string TargetChainId { get; set; }
    public string OriginToken { get; set; }
    public string QueryToAddress { get; set; }
    public string EthereumBridgeOutContractAddress { get; set; }
    public string EthereumSwapId { get; set; }
}