using System.Numerics;
using AElf.Types;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AElf.EventHandler
{
    [FunctionOutput]
    public class ReceiptInfo : IFunctionOutputDTO
    {
        [Parameter("bytes32", 1)] public byte[] ReceiptId { get; set; }

        [Parameter("string", 2)] public string TargetAddress { get; set; }

        [Parameter("uint256", 3)] public BigInteger Amount { get; set; }

        public override string ToString()
        {
            if (ReceiptId == null)
            {
                return "Receipt Id is null";
            }

            return $"{Hash.LoadFromByteArray(ReceiptId)}, {Address.FromBase58(TargetAddress)}, {Amount.ToString()}";
        }
    }
}