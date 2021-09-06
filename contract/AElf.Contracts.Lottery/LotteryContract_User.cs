using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Lottery
{
    public partial class LotteryContract
    {
        public override OwnLottery Stake(Int64Value input)
        {
            Assert(input.Value > 0, "Invalid staking amount.");
            InvalidIfDebugAssert(Context.CurrentBlockTime < State.StakingShutdownTimestamp.Value, "Activity already finished.");
            if (State.CurrentPeriodId.Value == 1)
            {
                InvalidIfDebugAssert(Context.CurrentBlockTime >= State.StakingStartTimestamp.Value, "Activity not started yet.");
            }

            var transferAmount = input.Value.Add(State.TxFee.Value?.StakeTxFee ?? 0);

            // Stake ELF Tokens.
            var ownLottery = State.OwnLotteryMap[Context.Sender] ?? new OwnLottery();
            var supposedLotteryAmount = CalculateSupposedLotteryAmount(ownLottery, input.Value);
            var newLotteryAmount = supposedLotteryAmount.Sub(ownLottery.LotteryCodeList.Count);
            Assert(newLotteryAmount >= 0, "Incorrect state.");
            
            ownLottery.TotalStakingAmount = ownLottery.TotalStakingAmount.Add(input.Value);

            if (newLotteryAmount == 0)
            {
                // Just update LotteryList.TotalStakingAmount
                State.OwnLotteryMap[Context.Sender] = ownLottery;
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = Context.Self,
                    Amount = transferAmount,
                    Symbol = TokenSymbol,
                    Memo = "No new lottery code."
                });
                return ownLottery;
            }

            var newLotteryCodeList = newLotteryAmount == 1
                ? new List<long> { State.CurrentLotteryCode.Value }
                : Enumerable.Range((int)State.CurrentLotteryCode.Value, newLotteryAmount).Select(i => (long)i).ToList();
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = transferAmount,
                Symbol = TokenSymbol,
                Memo = newLotteryAmount == 1
                    ? $"Got lottery with code {newLotteryCodeList.First()}"
                    : $"Got lotteries with code from {newLotteryCodeList.First()} to {newLotteryCodeList.Last()}"
            });

            foreach (var newLotteryCode in newLotteryCodeList)
            {
                var lottery = new Lottery
                {
                    LotteryCode = newLotteryCode,
                    IssueTimestamp = Context.CurrentBlockTime,
                    Owner = Context.Sender
                };

                ownLottery.LotteryCodeList.Add(newLotteryCode);

                // Update Lottery Map.
                State.LotteryMap[lottery.LotteryCode] = lottery;
            }

            State.CurrentLotteryCode.Value = State.CurrentLotteryCode.Value.Add(newLotteryAmount);

            // Update LotteryList Map.
            State.OwnLotteryMap[Context.Sender] = ownLottery;

            Context.Fire(new Staked
            {
                User = Context.Sender,
                Amount = input.Value,
                LotteryCodeList = new Int64List
                {
                    Value = { ownLottery.LotteryCodeList }
                }
            });

            if (State.TxFee.Value != null &&  State.TxFee.Value.StakeTxFee > 0)
            {
                Context.Fire(new TransactionFeeCharged
                {
                    Amount = State.TxFee.Value.StakeTxFee,
                    Symbol = Context.Variables.NativeSymbol
                });
            }
            return ownLottery;
        }

        public override Empty Claim(Empty input)
        {
            var periodAward = State.PeriodAwardMap[State.CurrentPeriodId.Value];
            Assert(periodAward.DrewAwardId == 0 || periodAward.DrewAwardId == periodAward.EndAwardId,
                "Cannot claim awards during drawing.");
            var ownLottery = State.OwnLotteryMap[Context.Sender];
            if (ownLottery == null)
            {
                throw new AssertionException("Sender doesn't own any lottery.");
            }

            var claimingAmount = 0L;
            foreach (var lotteryCode in ownLottery.LotteryCodeList)
            {
                var lottery = State.LotteryMap[lotteryCode];
                foreach (var awardId in lottery.AwardIdList)
                {
                    if (awardId <= lottery.LatestClaimedAwardId)
                    {
                        continue;
                    }

                    var award = State.AwardMap[awardId];
                    award.IsClaimed = true;
                    State.AwardMap[awardId] = award;

                    // Double check.
                    Assert(award.LotteryCode == lottery.LotteryCode && award.Owner == lottery.Owner,
                        $"Some wrong with the Award Id {awardId} and Lottery Code {lottery.LotteryCode}.\n{award}\n{lottery}");
                    claimingAmount = claimingAmount.Add(award.AwardAmount);

                    lottery.LatestClaimedAwardId = awardId;
                }

                State.LotteryMap[lotteryCode] = lottery;
            }

            Assert(ownLottery.TotalAwardAmount.Sub(ownLottery.ClaimedAwardAmount) == claimingAmount,
                $"Incorrect claiming award amount {claimingAmount}. OwnLottery: {ownLottery}");

            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = Context.Sender,
                Symbol = TokenSymbol,
                Amount = claimingAmount.Sub(State.TxFee.Value?.ClaimTxFee ?? 0),
                Memo = "Awards"
            });

            ownLottery.ClaimedAwardAmount = ownLottery.TotalAwardAmount;

            State.OwnLotteryMap[Context.Sender] = ownLottery;

            Context.Fire(new Claimed
            {
                User = Context.Sender,
                Amount = claimingAmount,
                PeriodId = State.CurrentPeriodId.Value,
                ClaimedLotteryCodeList = new Int64List
                {
                    Value = { ownLottery.LotteryCodeList }
                }
            });

            if (State.TxFee.Value != null && State.TxFee.Value.ClaimTxFee > 0)
            {
                Context.Fire(new TransactionFeeCharged
                {
                    Amount = State.TxFee.Value.ClaimTxFee,
                    Symbol = Context.Variables.NativeSymbol
                });
            }
            return new Empty();
        }

        public override Empty Redeem(Empty input)
        {
            InvalidIfDebugAssert(Context.CurrentBlockTime >= State.RedeemTimestamp.Value,
                $"Cannot redeem before {State.RedeemTimestamp.Value}.");
            InvalidIfDebugAssert(Context.CurrentBlockTime < State.StopRedeemTimestamp.Value,
                $"Cannot redeem after {State.StopRedeemTimestamp.Value}");
            var ownLottery = State.OwnLotteryMap[Context.Sender];
            if (ownLottery == null)
            {
                throw new AssertionException("Sender didn't stake.");
            }

            Assert(!ownLottery.IsRedeemed, "Already redeemed.");
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = Context.Sender,
                Symbol = TokenSymbol,
                Amount = ownLottery.TotalStakingAmount.Sub(State.TxFee.Value?.RedeemTxFee ?? 0),
                Memo = "Redeem staked tokens."
            });

            ownLottery.IsRedeemed = true;
            State.OwnLotteryMap[Context.Sender] = ownLottery;

            Context.Fire(new Redeemed
            {
                User = Context.Sender,
                Amount = ownLottery.TotalStakingAmount,
                PeriodId = State.CurrentPeriodId.Value
            });

            if (State.TxFee.Value != null && State.TxFee.Value.RedeemTxFee > 0)
            {
                Context.Fire(new TransactionFeeCharged
                {
                    Amount = State.TxFee.Value.RedeemTxFee,
                    Symbol = Context.Variables.NativeSymbol
                });
            }
            return new Empty();
        }

        private int CalculateSupposedLotteryAmount(OwnLottery ownLottery, long additionalStakingAmount)
        {
            var totalStakingAmount = ownLottery.TotalStakingAmount.Add(additionalStakingAmount);
            var remainStakingAmount = totalStakingAmount;
            remainStakingAmount = remainStakingAmount.Sub(AmountOfElfToGetFirstLotteryCode);
            if (remainStakingAmount < 0)
            {
                return 0;
            }

            return Math.Min(MaximumLotteryCodeAmountForSingleAddress,
                (int)remainStakingAmount.Div(AmountOfElfToGetMoreLotteryCode)
                    .Add(1) // Add the first lottery code which cost AmountOfElfToGetFirstLotteryCode ELF tokens.
            );
        }
    }
}