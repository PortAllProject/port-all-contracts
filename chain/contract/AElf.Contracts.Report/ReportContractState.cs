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
        public MappedState<string, long, Address, string> ObserverSignatureMap { get; set; }
        public MappedState<Address, long> CurrentRoundIdMap { get; set; }

        /// <summary>
        /// Ethereum Contract Address -> Round Number (Round Id) -> Report.
        /// </summary>
        public MappedState<string, long, Report> ReportMap { get; set; }

        public MappedState<string, OffChainAggregatorContractInfo> OffChainAggregatorContractInfoMap { get; set; }

        public MappedState<Address, long> ObserverMortgagedTokensMap { get; set; }

        public MappedState<string, long, BinaryMerkleTree> BinaryMerkleTreeMap { get; set; }
    }
}