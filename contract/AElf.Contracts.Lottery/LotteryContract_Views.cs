using System;
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
                Value = {awardIdList.Select(i => State.AwardMap[i])}
            };
        }

        public override Int64Value GetTotalLotteryCount(Empty input)
        {
            return new Int64Value
            {
                Value = State.CurrentLotteryCode.Value.Sub(1)
            };
        }

        public override Int64Value GetTotalAwardCount(Empty input)
        {
            return new Int64Value
            {
                Value = State.CurrentAwardId.Value
            };
        }

        public override Int64List GetLotteryCodeListByUserAddress(Address input)
        {
            var ownLottery = State.OwnLotteryMap[input];
            return ownLottery == null
                ? new Int64List()
                : new Int64List
                {
                    Value = {ownLottery.LotteryCodeList}
                };
        }

        public override Int32Value GetCurrentPeriodId(Empty input)
        {
            return new Int32Value {Value = State.CurrentPeriodId.Value};
        }

        public override Int64Value GetStakingAmount(Address input)
        {
            var ownLottery = GetOwnLottery(input);
            return ownLottery == null ? new Int64Value() : new Int64Value {Value = ownLottery.TotalStakingAmount};
        }

        public override AwardList GetAwardList(GetAwardListInput input)
        {
            var periodAward = State.PeriodAwardMap[input.PeriodId];
            if (periodAward == null)
            {
                return new AwardList();
            }

            var awardCount = periodAward.EndAwardId.Sub(periodAward.StartAwardId).Add(1);
            var maxCount = (int) awardCount.Sub(input.StartIndex);
            if (input.Count == 0)
            {
                input.Count = maxCount;
            }

            var awardIdList = Enumerable.Range((int) periodAward.StartAwardId.Add(input.StartIndex),
                Math.Min(maxCount, input.Count));
            return new AwardList
            {
                Value = {awardIdList.Select(id => State.AwardMap[id])}
            };
        }

        public override AwardAmountMap GetAwardAmountMap(Address input)
        {
            var awardList = GetAwardListByUserAddress(input);
            var awardAmountMap = new AwardAmountMap();
            var ownLottery = State.OwnLotteryMap[input];
            if (ownLottery == null)
            {
                return awardAmountMap;
            }

            foreach (var lotteryCode in ownLottery.LotteryCodeList)
            {
                awardAmountMap.Value.Add(lotteryCode,
                    awardList.Value.Where(a => a.LotteryCode == lotteryCode).Sum(a => a.AwardAmount));
            }

            return awardAmountMap;
        }

        public override Timestamp GetStartTimestamp(Empty input)
        {
            return State.StakingStartTimestamp.Value;
        }

        public override Timestamp GetShutdownTimestamp(Empty input)
        {
            return State.StakingShutdownTimestamp.Value;
        }

        public override Timestamp GetRedeemTimestamp(Empty input)
        {
            return State.RedeemTimestamp.Value;
        }

        public override Timestamp GetStopRedeemTimestamp(Empty input)
        {
            return State.StopRedeemTimestamp.Value;
        }

        public override TxFee GetTxFee(Empty input)
        {
            return State.TxFee.Value;
        }

        public override Address GetAdmin(Empty input)
        {
            return State.Admin.Value;
        }

        public override PeriodAward GetPreviousPeriodAward(Empty input)
        {
            var previousPeriodId = State.CurrentPeriodId.Value.Sub(1);
            if (previousPeriodId == 0)
            {
                return new PeriodAward();
            }

            return State.PeriodAwardMap[previousPeriodId];
        }

        public override PeriodAward GetCurrentPeriodAward(Empty input)
        {
            return State.PeriodAwardMap[State.CurrentPeriodId.Value];
        }
    }
}