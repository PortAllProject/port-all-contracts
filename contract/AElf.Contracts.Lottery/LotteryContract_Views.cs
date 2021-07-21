using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Lottery
{
    public partial class LotteryContract
    {
        public override Lottery GetLottery(Int64Value input)
        {
            return State.LotteryMap[input.Value];
        }

        public override Int64Value GetLotteryCount(Empty input)
        {
            return new Int64Value
            {
                Value = State.CurrentLotteryCode.Value
            };
        }

        public override OwnLottery GetOwnLottery(Address input)
        {
            return State.OwnLotteryMap[input];
        }

        public override Int64List GetLotteryCodeList(Address input)
        {
            return new Int64List
            {
                Value = { State.OwnLotteryMap[input].LotteryCodeList }
            };
        }
    }
}