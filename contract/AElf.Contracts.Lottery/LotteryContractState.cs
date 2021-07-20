using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using AElf.Contracts.TokenHolder;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Lottery
{
    public partial class LotteryContractState : ContractState
    {
        public StringState TokenSymbol { get; set; }

        public SingletonState<long> Price { get; set; }

        public SingletonState<long> MaximumAmount { get; set; }

        public SingletonState<long> DrawingLag { get; set; }

        public SingletonState<Address> Admin { get; set; }

        public SingletonState<long> CurrentPeriod { get; set; }

        public SingletonState<long> SelfIncreasingIdForLottery { get; set; }

        public MappedState<long, PeriodBody> Periods { get; set; }

        public MappedState<long, Lottery> Lotteries { get; set; }

        public MappedState<Address, long, LotteryList> OwnerToLotteries { get; set; }

        public MappedState<string, string> RewardMap { get; set; }

        public SingletonState<StringList> RewardCodeList { get; set; }

        public MappedState<Address, long> BoughtLotteriesCount { get; set; }

        public MappedState<Address, long> Staking { get; set; }

        public Int64State StakingTotal { get; set; }
        public Int64State DividendRate { get; set; }
        public Int64State DividendRateTotalShares { get; set; }

        public Int64State ProfitRate { get; set; }

        public BoolState IsSuspend { get; set; }
        
        public SingletonState<Timestamp> StakingStartTimestamp { get; set; }
        public SingletonState<Timestamp> StakingShutdownTimestamp { get; set; }
        
        public Int32State BoughtLotteryReturnLimit { get; set; }
    }
}