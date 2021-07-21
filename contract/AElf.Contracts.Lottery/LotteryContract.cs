using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Lottery
{
    public partial class LotteryContract : LotteryContractContainer.LotteryContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.TokenSymbol.Value == null, "Already initialized");
            State.TokenSymbol.Value = input.TokenSymbol;

            State.Admin.Value = Context.Sender;

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.RandomNumberProviderContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);

            State.Price.Value = input.Price == 0 ? DefaultPrice : input.Price;
            State.DrawingLag.Value = input.DrawingLag == 0 ? DefaultDrawingLag : input.DrawingLag;
            State.MaximumAmount.Value = input.MaximumAmount == 0 ? MaximumBuyAmount : input.MaximumAmount;
            State.SelfIncreasingIdForLottery.Value = 1;

            State.CurrentPeriod.Value = 1;
            State.Periods[1] = new PeriodBody
            {
                StartId = State.SelfIncreasingIdForLottery.Value,
                BlockNumber = Context.CurrentHeight.Add(State.DrawingLag.Value),
                RandomHash = Hash.Empty
            };

            Assert(input.StartTimestamp < input.ShutdownTimestamp, "Invalid staking timestamp.");
            Assert(Context.CurrentBlockTime < input.StartTimestamp, "Staking start timestamp already passed.");
            State.StakingShutdownTimestamp.Value = input.ShutdownTimestamp;
            State.StakingStartTimestamp.Value = input.StartTimestamp;
            
            Assert(input.ProfitsRate <= TotalSharesForProfitRate && input.ProfitsRate >= 0, "Invalid profit rate.");
            State.ProfitRate.Value = input.ProfitsRate;
            
            InitializeTokenHolderProfitScheme();
            return new Empty();
        }

        public override BoughtLotteriesInformation Buy(Int64Value input)
        {
            AssertIsNotSuspended();
            Assert(input.Value <= State.MaximumAmount.Value, $"Maximal amount limit {State.MaximumAmount.Value}.");
            Assert(input.Value > 0, "Minimal amount limit 1.");

            var currentPeriod = State.CurrentPeriod.Value;
            if (State.OwnerToLotteries[Context.Sender][currentPeriod] == null)
            {
                State.OwnerToLotteries[Context.Sender][currentPeriod] = new LotteryList();
            }

            State.BoughtLotteriesCount[Context.Sender] = State.BoughtLotteriesCount[Context.Sender].Add(input.Value);

            var amount = State.Price.Value.Mul(input.Value);
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = State.TokenSymbol.Value,
                Amount = amount
            });

            var startId = State.SelfIncreasingIdForLottery.Value;
            var newIds = new List<long>();
            for (var i = 0; i < input.Value; i++)
            {
                var selfIncreasingId = State.SelfIncreasingIdForLottery.Value;
                var lottery = new Lottery
                {
                    Id = selfIncreasingId,
                    Owner = Context.Sender,
                    Block = Context.CurrentHeight,
                };
                State.Lotteries[selfIncreasingId] = lottery;

                newIds.Add(selfIncreasingId);

                State.SelfIncreasingIdForLottery.Value = selfIncreasingId.Add(1);
            }

            var currentIds = State.OwnerToLotteries[Context.Sender][currentPeriod];
            currentIds.Ids.Add(newIds);
            Assert(currentIds.Ids.Count <= LotteryBoughtCountLimitInOnePeriod, "Too many lottery bought.");
            State.OwnerToLotteries[Context.Sender][currentPeriod] = currentIds;

            if (State.ProfitRate.Value > 0)
                ContributeProfits(amount.Mul(State.ProfitRate.Value).Div(TotalSharesForProfitRate));
            
            return new BoughtLotteriesInformation
            {
                StartId = startId,
                Amount = input.Value
            };
        }

        public override Empty PrepareDraw(Empty input)
        {
            AssertIsNotSuspended();
            Assert(Context.Sender == State.Admin.Value, "No permission to prepare!");

            // Check whether current period drew except period 1.
            if (State.CurrentPeriod.Value != 1)
            {
                Assert(State.Periods[State.CurrentPeriod.Value.Sub(1)].RandomHash != Hash.Empty,
                    $"Period {State.CurrentPeriod.Value} hasn't drew.");
            }

            var levelsCount = State.Periods[State.CurrentPeriod.Value].Rewards.Values.ToList();
            var rewardCount = levelsCount.Sum();
            Assert(rewardCount > 0, "Reward pool cannot be empty.");

            var poolCount =
                State.SelfIncreasingIdForLottery.Value.Sub(State.Periods[State.CurrentPeriod.Value].StartId);
            Assert(poolCount >= rewardCount, "Unable to prepare draw because not enough lottery sold.");

            State.CurrentPeriod.Value = State.CurrentPeriod.Value.Add(1);

            InitialNextPeriod();

            return new Empty();
        }

        public override Empty Draw(Int64Value input)
        {
            AssertIsNotSuspended();
            var currentPeriod = State.CurrentPeriod.Value;
            var previousPeriodBody = State.Periods[currentPeriod.Sub(1)];
            var currentPeriodBody = State.Periods[currentPeriod];

            Assert(input.Value.Add(1) == currentPeriod, "Incorrect period.");
            Assert(currentPeriod > 1, "Not ready to draw.");
            Assert(Context.Sender == State.Admin.Value, "No permission to draw!");
            Assert(previousPeriodBody.RandomHash == Hash.Empty, "Latest period already drawn.");
            Assert(
                previousPeriodBody.SupposedDrawDate == null ||
                previousPeriodBody.SupposedDrawDate.ToDateTime().DayOfYear >=
                Context.CurrentBlockTime.ToDateTime().DayOfYear,
                "Invalid draw date.");

            var expectedBlockNumber = currentPeriodBody.BlockNumber;
            Assert(Context.CurrentHeight >= expectedBlockNumber, "Block height not enough.");

            if (previousPeriodBody.Rewards == null || !previousPeriodBody.Rewards.Any())
            {
                throw new AssertionException("Reward list is empty.");
            }

            var randomBytes = State.RandomNumberProviderContract.GetRandomBytes.Call(new Int64Value
            {
                Value = expectedBlockNumber
            }.ToBytesValue());
            var randomHash = HashHelper.ComputeFrom(randomBytes);

            // Deal with lotteries base on the random hash.
            DealWithLotteries(previousPeriodBody.Rewards.ToDictionary(r => r.Key, r => r.Value), randomHash);

            return new Empty();
        }

        public override Empty TakeReward(TakeRewardInput input)
        {
            AssertIsNotSuspended();
            var lottery = State.Lotteries[input.LotteryId];
            if (lottery == null)
            {
                throw new AssertionException("Lottery id not found.");
            }

            Assert(lottery.Owner == Context.Sender, "Unable to take this reward.");
            Assert(!string.IsNullOrEmpty(lottery.RewardName), "No reward.");
            Assert(!lottery.IsRewardTaken,
                $"Reward already taken：{State.Lotteries[input.LotteryId].RegistrationInformation}");

            State.Lotteries[input.LotteryId].RegistrationInformation = input.RegistrationInformation;
            State.Lotteries[input.LotteryId].IsRewardTaken = true;

            return new Empty();
        }

        public override Empty AddRewardList(RewardList input)
        {
            AssertSenderIsAdmin();
            foreach (var map in input.RewardMap)
            {
                State.RewardMap[map.Key] = map.Value;
            }

            if (State.RewardCodeList.Value == null)
            {
                State.RewardCodeList.Value = new StringList {Value = {input.RewardMap.Keys}};
            }
            else
            {
                State.RewardCodeList.Value.Value.AddRange(input.RewardMap.Keys);
            }

            return new Empty();
        }

        public override Empty SetRewardListForOnePeriod(RewardsInfo input)
        {
            AssertSenderIsAdmin();
            var periodBody = State.Periods[input.Period];
            Assert(periodBody.RandomHash == Hash.Empty, "This period already terminated.");

            periodBody.Rewards.Clear();
            periodBody.Rewards.Add(input.Rewards);
            periodBody.SupposedDrawDate = input.SupposedDrawDate;

            State.Periods[input.Period] = periodBody;
            return new Empty();
        }

        public override Empty ResetPrice(Int64Value input)
        {
            AssertSenderIsAdmin();
            State.Price.Value = input.Value;
            return new Empty();
        }

        public override Empty ResetDrawingLag(Int64Value input)
        {
            AssertSenderIsAdmin();
            State.DrawingLag.Value = input.Value;
            return new Empty();
        }

        public override Empty ResetMaximumBuyAmount(Int64Value input)
        {
            AssertSenderIsAdmin();
            State.MaximumAmount.Value = input.Value;
            return new Empty();
        }

        public override Empty Suspend(Empty input)
        {
            AssertSenderIsAdmin();
            State.IsSuspend.Value = true;
            return new Empty();
        }

        public override Empty Recover(Empty input)
        {
            AssertSenderIsAdmin();
            State.IsSuspend.Value = false;
            return new Empty();
        }

        public override Empty Stake(Int64Value input)
        {
            Assert(Context.CurrentBlockTime > State.StakingStartTimestamp.Value, "Staking not started.");
            Assert(Context.CurrentBlockTime < State.StakingShutdownTimestamp.Value, "Staking shutdown.");
            
            State.Staking[Context.Sender] = State.Staking[Context.Sender].Add(input.Value);
            State.StakingTotal.Value = State.StakingTotal.Value.Add(input.Value);
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = State.TokenSymbol.Value,
                Amount = input.Value
            });

            return new Empty();
        }

        public override Empty SetStakingTimestamp(SetStakingTimestampInput input)
        {
            Assert(State.Admin.Value == Context.Sender, "No permission.");
            if (input.IsStartTimestamp)
            {
                Assert(
                    State.StakingStartTimestamp.Value == null ||
                    State.StakingStartTimestamp.Value > Context.CurrentBlockTime, "Start timestamp already passed.");
                Assert(
                    Context.CurrentBlockTime < input.Timestamp &&
                    input.Timestamp < State.StakingShutdownTimestamp.Value, "Invalid start timestamp.");
                State.StakingStartTimestamp.Value = input.Timestamp;
            }
            else
            {
                Assert(
                    input.Timestamp > State.StakingStartTimestamp.Value && input.Timestamp > Context.CurrentBlockTime,
                    "Invalid shutdown timestamp.");

                State.StakingShutdownTimestamp.Value = input.Timestamp;
            }

            return new Empty();
        }
        
        public override Empty TakeBackToken(TakeBackTokenInput input)
        {
            Assert(Context.Sender == State.Admin.Value, "No permission.");
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Amount = input.Amount,
                Symbol = input.Symbol,
                To = State.Admin.Value
            });
            
            return new Empty();
        }

        public override Empty SetProfitsRate(Int64Value input)
        {
            Assert(Context.Sender == State.Admin.Value, "No permission.");
            Assert(input.Value <= TotalSharesForProfitRate && input.Value >= 0, "Invalid profit rate.");
            State.ProfitRate.Value = input.Value;
            return new Empty();
        }

        public override Empty ChangeBoughtLotteryReturnLimit(Int32Value input)
        {
            Assert(Context.Sender == State.Admin.Value, "No permission.");
            Assert(input.Value >= 0, "BoughtLotteryReturnLimit cannot be negative.");
            State.BoughtLotteryReturnLimit.Value = input.Value;
            return new Empty();
        }
        
        public override Empty TakeDividend(Empty input)
        {
            Assert(State.DividendRate.Value > 0, "DividendRate not set.");
            Assert(Context.CurrentBlockTime > State.StakingShutdownTimestamp.Value, "Staking not shutdown.");
            Assert(State.Staking[Context.Sender] > 0, "No stake.");
            var amount = State.Staking[Context.Sender].Mul(State.DividendRate.Value)
                .Div(GetDividendRateTotalShares(new Empty()).Value);
            State.Staking[Context.Sender] = 0;
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Amount = amount,
                Symbol = "ELF",
                To = Context.Sender
            });

            return new Empty();
        }

        public override Empty SetDividendRate(Int64Value input)
        {
            Assert(Context.CurrentBlockTime > State.StakingShutdownTimestamp.Value, "Staking not shutdown.");
            Assert(Context.Sender == State.Admin.Value, "No permission.");
            Assert(input.Value >= 0, "DividendRate cannot be negative.");
            State.DividendRate.Value = input.Value;
            return new Empty();
        }

        public override Empty SetDividendRateTotalShares(Int64Value input)
        {
            Assert(Context.Sender == State.Admin.Value, "No permission.");
            Assert(input.Value > 0, "DividendRate cannot be zero or negative.");
            State.DividendRateTotalShares.Value = input.Value;
            return new Empty();
        }
    }
}