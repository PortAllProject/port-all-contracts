using System;
using System.Linq;
using AElf.Contracts.MultiToken;
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

            State.CachedAwardedLotteryCodeList.Value ??= new Int64List();

            var periodAward = State.PeriodAwardMap[input.PeriodId];

            if (input.ToAwardId != 0)
            {
                Assert(periodAward.StartAwardId < input.ToAwardId && input.ToAwardId <= periodAward.EndAwardId,
                    "Incorrect to award id.");

                if (periodAward.DrewAwardId != 0)
                {
                    Assert(input.ToAwardId > periodAward.DrewAwardId, "Incorrect to award id.");
                }
            }

            var randomBytes = State.RandomNumberProviderContract.GetRandomBytes.Call(new Int64Value
            {
                Value = Context.CurrentHeight.Sub(1)
            }.ToBytesValue());
            var randomHash =
                HashHelper.ConcatAndCompute(Context.PreviousBlockHash, HashHelper.ComputeFrom(randomBytes));
            periodAward.UsedRandomHashes.Add(randomHash);
            periodAward.EndTimestamp = Context.CurrentBlockTime;

            var startAwardId = periodAward.DrewAwardId == 0 ? periodAward.StartAwardId : periodAward.DrewAwardId.Add(1);
            var endAwardId = input.ToAwardId == 0 || input.ToAwardId == periodAward.EndAwardId
                ? periodAward.EndAwardId
                : input.ToAwardId;
            var totalLotteryCount = GetTotalLotteryCount(new Empty()).Value;
            var awardCount = periodAward.EndAwardId.Sub(periodAward.StartAwardId).Sub(1);
            var actualEndAwardId = totalLotteryCount > awardCount.Mul(2)
                ? DoDrawForLotteryCodeEnough(startAwardId, endAwardId, randomHash)
                : DoDrawForLotteryCodeNotEnough(startAwardId, endAwardId, randomHash);
            periodAward.DrewAwardId = actualEndAwardId;

            State.CurrentAwardId.Value = actualEndAwardId;

            if (input.ToAwardId == 0 || input.ToAwardId == periodAward.EndAwardId)
            {
                for (var i = actualEndAwardId.Add(1); i <= periodAward.EndAwardId; i++)
                {
                    State.AwardMap.Remove(i);
                }

                periodAward.EndAwardId = actualEndAwardId;

                var newPeriodId = State.CurrentPeriodId.Value.Add(1);
                State.PeriodAwardMap[newPeriodId] = GenerateNextPeriodAward(
                    input.NextAwardList == null || input.NextAwardList.Any()
                        ? new Int64List {Value = {input.NextAwardList}}
                        : null);

                State.CurrentPeriodId.Value = State.CurrentPeriodId.Value.Add(1);

                State.CachedAwardedLotteryCodeList.Value = new Int64List();
                Context.Fire(new DrewFinished {PeriodId = input.PeriodId});
            }
            else
            {
                Context.Fire(new DrewUnfinished
                {
                    PeriodId = State.CurrentPeriodId.Value,
                    ToAwardId = Math.Min(input.ToAwardId, actualEndAwardId)
                });
            }

            State.PeriodAwardMap[input.PeriodId] = periodAward;

            return new Empty();
        }

        /// <summary>
        /// Draw
        /// Will update State.LotteryMap & State.AwardMap & State.OwnLotteryMap
        /// </summary>
        /// <param name="startAwardId"></param>
        /// <param name="endAwardId"></param>
        /// <param name="randomHash"></param>
        /// <returns>the end award id</returns>
        private long DoDrawForLotteryCodeEnough(long startAwardId, long endAwardId, Hash randomHash)
        {
            var randomNumber = Context.ConvertHashToInt64(randomHash);

            var luckyLotteryCode = Math.Abs(randomNumber % State.CurrentLotteryCode.Value.Sub(1)).Add(1);
            for (var awardId = startAwardId; awardId <= endAwardId; awardId++)
            {
                while (State.CachedAwardedLotteryCodeList.Value.Value.Contains(luckyLotteryCode))
                {
                    // Keep updating lucky lottery code.
                    randomHash = HashHelper.ComputeFrom(randomHash);
                    randomNumber = Context.ConvertHashToInt64(randomHash);
                    luckyLotteryCode = Math.Abs(randomNumber % State.CurrentLotteryCode.Value.Sub(1)).Add(1);
                }

                // Bind lottery id & award id in both sides.
                var award = State.AwardMap[awardId];

                var lottery = State.LotteryMap[luckyLotteryCode];
                lottery.AwardIdList.Add(award.AwardId);
                lottery.LotteryTotalAwardAmount = lottery.LotteryTotalAwardAmount.Add(award.AwardAmount);
                State.LotteryMap[luckyLotteryCode] = lottery;

                award.LotteryCode = luckyLotteryCode;
                award.Owner = lottery.Owner;
                State.AwardMap[awardId] = award;

                var ownLottery = State.OwnLotteryMap[lottery.Owner];
                ownLottery.TotalAwardAmount = ownLottery.TotalAwardAmount.Add(award.AwardAmount);
                State.OwnLotteryMap[lottery.Owner] = ownLottery;

                State.CachedAwardedLotteryCodeList.Value.Value.Add(luckyLotteryCode);
            }

            return endAwardId;
        }

        private long DoDrawForLotteryCodeNotEnough(long startAwardId, long endAwardId, Hash randomHash)
        {
            if (State.CurrentLotteryCode.Value == 1)
            {
                return startAwardId;
            }

            var lotteryCodePool = Enumerable.Range(1, (int) GetTotalLotteryCount(new Empty()).Value).ToList();
            foreach (var lotteryCode in State.CachedAwardedLotteryCodeList.Value.Value)
            {
                lotteryCodePool.Remove((int) lotteryCode);
            }

            for (var awardId = startAwardId; awardId <= endAwardId; awardId++)
            {
                if (State.CachedAwardedLotteryCodeList.Value.Value.Count >= GetTotalLotteryCount(new Empty()).Value)
                {
                    // Award count is greater than lottery code count, no need to draw.
                    return awardId.Sub(1);
                }

                var randomNumber = Context.ConvertHashToInt64(randomHash);
                if (lotteryCodePool.Count == 0)
                {
                    Assert(lotteryCodePool.Count != 0, "Lottery code pool is empty.");
                }

                var luckyLotteryCodeIndex = (int) Math.Abs(randomNumber % lotteryCodePool.Count);
                var luckyLotteryCode = lotteryCodePool[luckyLotteryCodeIndex];
                lotteryCodePool.Remove(luckyLotteryCode);

                // Update random hash each time.
                randomHash = HashHelper.ComputeFrom(randomHash);

                // Bind lottery id & award id in both sides.
                var award = State.AwardMap[awardId];

                var lottery = State.LotteryMap[luckyLotteryCode];
                lottery.AwardIdList.Add(award.AwardId);
                lottery.LotteryTotalAwardAmount = lottery.LotteryTotalAwardAmount.Add(award.AwardAmount);
                State.LotteryMap[luckyLotteryCode] = lottery;

                award.LotteryCode = luckyLotteryCode;
                award.Owner = lottery.Owner;
                State.AwardMap[awardId] = award;

                var ownLottery = State.OwnLotteryMap[lottery.Owner];
                ownLottery.TotalAwardAmount = ownLottery.TotalAwardAmount.Add(award.AwardAmount);
                State.OwnLotteryMap[lottery.Owner] = ownLottery;

                State.CachedAwardedLotteryCodeList.Value.Value.Add(luckyLotteryCode);
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

            if (input.StopRedeemTimestamp != null)
            {
                State.StopRedeemTimestamp.Value = input.StopRedeemTimestamp;
            }

            AssertTimestampOrder();

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
                awardAmountList = new Int64List {Value = {GetDefaultAwardList()}};
            }

            var currentAwardId = State.CurrentAwardId.Value;
            for (var i = 0; i < awardAmountList.Value.Count; i++)
            {
                State.AwardMap[currentAwardId.Add(i).Add(1)] = new Award
                {
                    AwardId = currentAwardId.Add(i).Add(1),
                    AwardAmount = awardAmountList.Value[i],
                    Period = State.CurrentPeriodId.Value.Add(1)
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

        public override Empty Withdraw(Int64Value input)
        {
            AssertSenderIsAdmin();
            var totalAmount = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = Context.Self,
                Symbol = Context.Variables.NativeSymbol
            }).Balance;
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = State.Admin.Value,
                Symbol = TokenSymbol,
                Amount = totalAmount,
                Memo = "Take all awards."
            });
            return new Empty();
        }

        public override Empty ResetTxFee(TxFee input)
        {
            AssertSenderIsAdmin();
            Assert(input != null, "TxFee cannot be null.");
            State.TxFee.Value = input;
            return new Empty();
        }

        public override Empty ResetAdmin(Address input)
        {
            AssertSenderIsAdmin();
            State.Admin.Value = input;
            return new Empty();
        }
    }
}