using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Report
{
    public partial class ReportContractState : ContractState
    {
        public SingletonState<Address> ReportControllerAddress { get; set; }
        public SingletonState<string> OracleTokenSymbol { get; set; }
        public SingletonState<string> ObserverMortgageTokenSymbol { get; set; }
        public SingletonState<long> ReportFee { get; set; }
        public SingletonState<long> ApplyObserverFee { get; set; }
        public MappedState<Hash, ReportQueryRecord> ReportQueryRecordMap { get; set; }
        public MappedState<Address, long, Address, string> ObserverSignatureMap { get; set; }
        public MappedState<Address, long> CurrentEpochMap { get; set; }
        public MappedState<Address, long> CurrentRoundIdMap { get; set; }

        /// <summary>
        /// Observer Association Address -> Round Number (Round Id) -> Report.
        /// </summary>
        public MappedState<Address, long, Report> ReportMap { get; set; }

        public MappedState<Address, OffChainAggregatorContractInfo> OffChainAggregatorContractInfoMap { get; set; }

        public MappedState<Address, long> ObserverMortgagedTokensMap { get; set; }
    }
}