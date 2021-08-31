using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Lottery
{
    public partial class LotteryContractState : ContractState
    {
        public SingletonState<Address> Admin { get; set; }

        public SingletonState<int> CurrentPeriodId { get; set; }
        public SingletonState<long> CurrentLotteryCode { get; set; }
        public SingletonState<long> CurrentAwardId { get; set; }

        public MappedState<long, PeriodAward> PeriodAwardMap { get; set; }

        // ReSharper disable once InconsistentNaming
        public SingletonState<Int64List> DefaultPeriodAwardAmountList { get; set; }

        /// <summary>
        /// Lottery Code -> Lottery
        /// </summary>
        public MappedState<long, Lottery> LotteryMap { get; set; }

        /// <summary>
        /// Award Id -> Award
        /// </summary>
        public MappedState<long, Award> AwardMap { get; set; }

        /// <summary>
        /// Address - OwnLottery
        /// </summary>
        public MappedState<Address, OwnLottery> OwnLotteryMap { get; set; }

        public SingletonState<Timestamp> StakingStartTimestamp { get; set; }
        public SingletonState<Timestamp> StakingShutdownTimestamp { get; set; }
        public SingletonState<Timestamp> RedeemTimestamp { get; set; }
        public SingletonState<Timestamp> StopRedeemTimestamp { get; set; }

        public BoolState IsDebug { get; set; }

        public SingletonState<Int64List> CachedAwardedLotteryCodeList { get; set; }

        public SingletonState<TxFee> TxFee { get; set; }
    }
}