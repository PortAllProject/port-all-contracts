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

            var defaultAwardList = input.DefaultAwardList == null || !input.DefaultAwardList.Any()
                ? GetDefaultAwardList()
                : input.DefaultAwardList.ToList();
            State.Admin.Value = input.Admin ?? Context.Sender;
            State.DefaultPeriodAwardAmountList.Value = new Int64List {Value = {defaultAwardList}};

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.RandomNumberProviderContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);

            State.PeriodAwardMap[1] = GenerateNextPeriodAward(new Int64List {Value = {defaultAwardList}},
                input.StartTimestamp);
            State.CurrentAwardId.Value = State.PeriodAwardMap[1].EndAwardId;
            State.CurrentPeriodId.Value = 1;
            State.CurrentLotteryCode.Value = 1;

            Assert(Context.CurrentBlockTime < input.StartTimestamp, "Invalid start timestamp.");
            State.StakingStartTimestamp.Value = input.StartTimestamp;
            State.StakingShutdownTimestamp.Value = input.ShutdownTimestamp;
            State.RedeemTimestamp.Value = input.RedeemTimestamp;
            State.StopRedeemTimestamp.Value = input.StopRedeemTimestamp;
            AssertTimestampOrder();
            State.CachedAwardedLotteryCodeList.Value = new Int64List();

            if (input.TxFee != null)
            {
                State.TxFee.Value = input.TxFee;
            }

            State.IsDebug.Value = input.IsDebug;
            return new Empty();
        }

        private void AssertTimestampOrder()
        {
            Assert(State.StakingStartTimestamp.Value < State.StakingShutdownTimestamp.Value, "Invalid shutdown timestamp.");
            Assert(State.StakingShutdownTimestamp.Value <= State.RedeemTimestamp.Value, "Invalid redeem timestamp.");
            Assert(State.RedeemTimestamp.Value <= State.StopRedeemTimestamp.Value, "Invalid stop redeem timestamp.");
        }

        private void InvalidIfDebugAssert(bool asserted, string message)
        {
            if (!State.IsDebug.Value)
            {
                Assert(asserted, message);
            }
        }
    }
}