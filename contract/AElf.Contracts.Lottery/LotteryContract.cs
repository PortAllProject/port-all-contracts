using System.Linq;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Lottery
{
    public partial class LotteryContract : LotteryContractContainer.LotteryContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.Admin.Value == null, "Already initialized");

            var defaultAwardList = input.DefaultAwardList?.ToList() ?? GetDefaultAwardList();
            State.Admin.Value = Context.Sender;
            State.PeriodAwardAmountList.Value = new Int64List { Value = { defaultAwardList } };

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.RandomNumberProviderContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);

            State.PeriodAwardMap[1] = GenerateNextPeriodAward(new Int64List { Value = { defaultAwardList } },
                input.StartTimestamp);
            State.CurrentAwardId.Value = State.PeriodAwardMap[1].EndAwardId;
            State.CurrentPeriodId.Value = 1;
            State.CurrentLotteryCode.Value = 1;

            Assert(input.StartTimestamp < input.ShutdownTimestamp, "Invalid staking timestamp.");
            Assert(Context.CurrentBlockTime < input.StartTimestamp, "Invalid start timestamp.");
            State.StakingStartTimestamp.Value = input.StartTimestamp;
            State.StakingShutdownTimestamp.Value = input.ShutdownTimestamp;
            State.RedeemTimestamp.Value = input.RedeemTimestamp;

            return new Empty();
        }
    }
}