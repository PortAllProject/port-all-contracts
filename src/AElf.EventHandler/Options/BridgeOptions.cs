using System.Collections.Generic;

namespace AElf.EventHandler;

public class BridgeOptions
{
    public bool IsSendQuery { get; set; }
    public bool IsTransmitter { get; set; }
    public long QueryPayment { get; set; }
    public List<BridgeItem> Bridges { get; set; }
}

public class BridgeItem
{
    public string EthereumClientAlias { get; set; }
    public string OriginToken { get; set; }
    public string BridgeContractAddress { get; set; }
    public string SpaceId { get; set; }
    public string SwapId { get; set; }
    public string MaximumLeafCount { get; set; }
    public string QueryToAddress { get; set; }
}