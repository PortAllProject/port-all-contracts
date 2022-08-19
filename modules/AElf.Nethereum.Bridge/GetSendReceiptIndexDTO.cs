using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AElf.Nethereum.Bridge;

[FunctionOutput]
public class GetSendReceiptIndexDTO : IFunctionOutputDTO
{
    [Parameter("tuple[]", "indexs", 1)] public List<BigInteger> Indexes { get; set; }
}