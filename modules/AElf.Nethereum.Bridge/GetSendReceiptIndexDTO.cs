using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AElf.Nethereum.Bridge;

[FunctionOutput]
public class GetSendReceiptIndexDTO : IFunctionOutputDTO
{
    [Parameter("uint256[]", "indexes", 1)] 
    public List<BigInteger> Indexes { get; set; }
}