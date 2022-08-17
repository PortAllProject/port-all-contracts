using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AElf.Nethereum.Bridge;

[FunctionOutput]
public class GetReceiptInfosDTO: IFunctionOutputDTO
{
    [Parameter("tuple[]", "_receipts", 1)]
    public List<ReceiptDTO> Receipts { get; set; }
}

[FunctionOutput]
public class ReceiptDTO : IFunctionOutputDTO
{
    [Parameter("uint256", "receiptId", 1)]
    public BigInteger ReceiptId { get; set; }

    [Parameter("address", "asset", 2)]
    public string Asset { get; set; }
    
    [Parameter("address", "owner", 3)]
    public string Owner { get; set; }
    
    [Parameter("string", "targetChainId", 4)]
    public string TargetChainId { get; set; }
    
    [Parameter("string", "targetAddress", 5)]
    public string TargetAddress { get; set; }

    [Parameter("uint256", "amount", 6)]
    public BigInteger Amount { get; set; }
    
    [Parameter("uint256", "blockHeight", 7)]
    public BigInteger BlockHeight { get; set; }
    
    [Parameter("uint256", "blockTime", 8)]
    public BigInteger BlockTime { get; set; }
}