using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
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

        public override Award GetAward(Int64Value input)
        {
            return State.AwardMap[input.Value];
        }

        public override OwnLottery GetOwnLottery(Address input)
        {
            return State.OwnLotteryMap[input];
        }

        public override PeriodAward GetPeriodAward(Int64Value input)
        {
            return State.PeriodAwardMap[input.Value];
        }

        public override AwardList GetAwardListByUserAddress(Address input)
        {
            var ownLottery = State.OwnLotteryMap[input];
            if (ownLottery == null)
            {
                return new AwardList();
            }

            var lotteryCodeList = ownLottery.LotteryCodeList;
            var lotteries = lotteryCodeList.Select(c => State.LotteryMap[c]);
            var awardIdList = new List<long>();
            foreach (var lottery in lotteries)
            {
                awardIdList.AddRange(lottery.AwardIdList);
            }

            return new AwardList
            {
                Value = { awardIdList.Select(i => State.AwardMap[i]) }
            };
        }

        public override Int64Value GetTotalLotteryCount(Empty input)
        {
            return new Int64Value
            {
                Value = State.CurrentLotteryCode.Value
            };
        }

        public override Int64List GetLotteryCodeListByUserAddress(Address input)
        {
            return new Int64List
            {
                Value = { State.OwnLotteryMap[input].LotteryCodeList }
            };
        }

        public override Int32Value GetCurrentPeriodId(Empty input)
        {
            return new Int32Value { Value = State.CurrentPeriodId.Value };
        }

        public override Int64Value GetStakingAmount(Address input)
        {
            var ownLottery = GetOwnLottery(input);
            return ownLottery == null ? new Int64Value() : new Int64Value { Value = ownLottery.TotalStakingAmount };
        }

        public override AwardList GetAwardListByPeriodId(Int64Value input)
        {
            var periodAward = State.PeriodAwardMap[input.Value];
            var awardList = Enumerable.Range((int)periodAward.StartAwardId,
                (int)periodAward.EndAwardId.Sub(periodAward.StartAwardId).Add(1));
            return new AwardList
            {
                Value = { awardList.Select(id => State.AwardMap[id]) }
            };
        }
    }
}