using System;
using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Lottery
{
    public partial class LotteryContract
    {
        public override Empty Draw(DrawInput input)
        {
            AssertSenderIsAdmin();
            Assert(State.CurrentPeriodId.Value == input.PeriodId && input.PeriodId <= TotalPeriod,
                "Incorrect period id.");

            var periodAward = State.PeriodAwardMap[input.PeriodId];
            var randomBytes = State.RandomNumberProviderContract.GetRandomBytes.Call(new Int64Value
            {
                Value = Context.CurrentHeight.Sub(1)
            }.ToBytesValue());
            var randomHash =
                HashHelper.ConcatAndCompute(Context.PreviousBlockHash, HashHelper.ComputeFrom(randomBytes));

            var actualEndAwardId = DoDraw(periodAward.StartAwardId, periodAward.EndAwardId, randomHash);

            periodAward.UseRandomHash = randomHash;
            periodAward.EndTimestamp = Context.CurrentBlockTime;
            periodAward.EndAwardId = actualEndAwardId;
            State.PeriodAwardMap[input.PeriodId] = periodAward;

            State.CurrentAwardId.Value = actualEndAwardId;

            for (var i = actualEndAwardId.Add(1); i <= periodAward.EndAwardId; i++)
            {
                State.AwardMap.Remove(i);
            }

            var newPeriodId = State.CurrentPeriodId.Value.Add(1);
            State.PeriodAwardMap[newPeriodId] = GenerateNextPeriodAward(
                input.NextAwardList == null || input.NextAwardList.Any()
                    ? new Int64List { Value = { input.NextAwardList } }
                    : null);

            Context.Fire(new Drew { PeriodId = State.CurrentPeriodId.Value });

            State.CurrentPeriodId.Value = State.CurrentPeriodId.Value.Add(1);

            return new Empty();
        }

        /// <summary>
        /// Draw
        /// </summary>
        /// <param name="startAwardId"></param>
        /// <param name="endAwardId"></param>
        /// <param name="randomHash"></param>
        /// <returns>the end award id</returns>
        private long DoDraw(long startAwardId, long endAwardId, Hash randomHash)
        {
            var randomNumber = Context.ConvertHashToInt64(randomHash);
            if (State.CurrentLotteryCode.Value == 1)
            {
                return startAwardId;
            }
            var luckyLotteryCode = Math.Abs(randomNumber % State.CurrentLotteryCode.Value.Sub(1)).Add(1);
            var awardedLotteryCodeList = new List<long>();
            for (var awardId = startAwardId; awardId <= endAwardId; awardId++)
            {
                var drewAwardCountThisPeriod = awardId.Sub(startAwardId);
                if (drewAwardCountThisPeriod >= GetTotalLotteryCount(new Empty()).Value)
                {
                    // Award count is greater than lottery code count, no need to draw.
                    return awardId.Sub(1);
                }

                while (awardedLotteryCodeList.Contains(luckyLotteryCode))
                {
                    // Keep updating lucky lottery code.
                    randomNumber = Context.ConvertHashToInt64(HashHelper.ConcatAndCompute(
                        HashHelper.ComputeFrom(randomNumber),
                        HashHelper.ComputeFrom(randomNumber)));
                    luckyLotteryCode = Math.Abs(randomNumber % State.CurrentLotteryCode.Value.Sub(1)).Add(1);
                }

                // Bind lottery id & award id in both sides.
                var award = State.AwardMap[awardId];

                var lottery = State.LotteryMap[luckyLotteryCode];
                lottery.AwardIdList.Add(award.AwardId);
                State.LotteryMap[luckyLotteryCode] = lottery;

                award.LotteryCode = luckyLotteryCode;
                award.Owner = lottery.Owner;
                State.AwardMap[awardId] = award;

                var ownLottery = State.OwnLotteryMap[lottery.Owner];
                ownLottery.TotalAwardAmount = ownLottery.TotalAwardAmount.Add(award.AwardAmount);
                State.OwnLotteryMap[lottery.Owner] = ownLottery;

                awardedLotteryCodeList.Add(luckyLotteryCode);
            }

            return endAwardId;
        }

        public override Empty ResetTimestamp(InitializeInput input)
        {
            AssertSenderIsAdmin();

            if (input.StartTimestamp != null)
            {
                State.StakingStartTimestamp.Value = input.StartTimestamp;
            }

            if (input.ShutdownTimestamp != null)
            {
                State.StakingShutdownTimestamp.Value = input.ShutdownTimestamp;
            }

            if (input.RedeemTimestamp != null)
            {
                State.RedeemTimestamp.Value = input.RedeemTimestamp;
            }

            return new Empty();
        }

        private void AssertSenderIsAdmin()
        {
            Assert(Context.Sender == State.Admin.Value, "Sender should be admin.");
        }

        /// <summary>
        /// Will update State.AwardMap
        /// </summary>
        /// <param name="awardAmountList"></param>
        /// <param name="startTimestamp"></param>
        /// <returns></returns>
        private PeriodAward GenerateNextPeriodAward(Int64List awardAmountList = null, Timestamp startTimestamp = null)
        {
            if (awardAmountList == null || !awardAmountList.Value.Any())
            {
                awardAmountList = State.DefaultPeriodAwardAmountList.Value;
            }

            var currentAwardId = State.CurrentAwardId.Value;
            for (var i = 0; i < awardAmountList.Value.Count; i++)
            {
                State.AwardMap[currentAwardId.Add(i).Add(1)] = new Award
                {
                    AwardId = currentAwardId.Add(i).Add(1),
                    AwardAmount = awardAmountList.Value[i],
                };
            }

            return new PeriodAward
            {
                PeriodId = State.CurrentPeriodId.Value.Add(1),
                StartTimestamp = startTimestamp ?? Context.CurrentBlockTime,
                StartAwardId = currentAwardId.Add(1),
                EndAwardId = currentAwardId.Add(awardAmountList.Value.Count)
            };
        }
    }
}