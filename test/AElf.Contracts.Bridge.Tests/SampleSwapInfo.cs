using System.Collections.Generic;
using System.Linq;
using AElf.Types;
using SampleAccount = AElf.ContractTestBase.ContractTestKit.SampleAccount;

namespace AElf.Contracts.Bridge.Tests
{
    public class SampleSwapInfo
    {
        public static List<SwapInfo> SwapInfos;

        static SampleSwapInfo()
        {
            SwapInfos = new List<SwapInfo>();
            for (var i = 1; i <= 100; i++)
            {
                SwapInfos.Add(new SwapInfo
                {
                    OriginAmount = (100000000000000000 * i).ToString(),
                    ReceiverAddress = Receivers[(i - 1) % 5],
                    ReceiptId = i - 1
                });
            }
        }

        private static readonly List<Address> Receivers =
            SampleAccount.Accounts.Skip(6).Take(5).Select(a => a.Address).ToList();
    }

    public class SwapInfo
    {
        public Hash ReceiptHash => CalculateReceiptHash();
        public long ReceiptId { get; set; }
        public Address ReceiverAddress { get; set; }
        public string OriginAmount { get; set; }

        private Hash CalculateReceiptHash()
        {
            var amountHash = GetHashTokenAmountData(decimal.Parse(OriginAmount), 32, true);
            var receiptIdHash = HashHelper.ComputeFrom(ReceiptId);
            var targetAddressHash = GetHashFromAddressData(ReceiverAddress);
            return HashHelper.ConcatAndCompute(amountHash, targetAddressHash, receiptIdHash);
        }

        private Hash GetHashTokenAmountData(decimal amount, int originTokenSizeInByte, bool isBigEndian)
        {
            var preHolderSize = originTokenSizeInByte - 16;
            int[] amountInIntegers;
            if (isBigEndian)
            {
                amountInIntegers = decimal.GetBits(amount).Reverse().ToArray();
                if (preHolderSize < 0)
                    amountInIntegers = amountInIntegers.TakeLast(originTokenSizeInByte / 4).ToArray();
            }
            else
            {
                amountInIntegers = decimal.GetBits(amount).ToArray();
                if (preHolderSize < 0)
                    amountInIntegers = amountInIntegers.Take(originTokenSizeInByte / 4).ToArray();
            }

            var amountBytes = new List<byte>();

            amountInIntegers.Aggregate(amountBytes, (cur, i) =>
            {
                cur.AddRange(i.ToBytes(isBigEndian));
                return cur;
            });

            if (preHolderSize > 0)
            {
                var placeHolder = Enumerable.Repeat(new byte(), preHolderSize).ToArray();
                amountBytes = isBigEndian
                    ? placeHolder.Concat(amountBytes).ToList()
                    : amountBytes.Concat(placeHolder).ToList();
            }

            return HashHelper.ComputeFrom(amountBytes.ToArray());
        }

        private Hash GetHashFromAddressData(Address receiverAddress)
        {
            return HashHelper.ComputeFrom(receiverAddress.ToBase58());
        }
    }
}