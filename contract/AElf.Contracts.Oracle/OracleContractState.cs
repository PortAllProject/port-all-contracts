using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Oracle
{
    /// <summary>
    /// The state class of the contract, it inherits from the AElf.Sdk.CSharp.State.ContractState type. 
    /// </summary>
    public partial class OracleContractState : ContractState
    {
        public SingletonState<bool> Initialized { get; set; }

        public SingletonState<int> DefaultExpirationSeconds { get; set; }

        public SingletonState<Address> Controller { get; set; }

        public SingletonState<int> AggregateThreshold { get; set; }

        public SingletonState<int> RevealThreshold { get; set; }

        public SingletonState<int> MinimumOracleNodesCount { get; set; }

        public MappedState<Hash, QueryRecord> QueryRecords { get; set; }

        public MappedState<Hash, int> ResponseCount { get; set; }

        public MappedState<Hash, Address, Hash> CommitmentMap { get; set; }

        public MappedState<Hash, ResultList> ResultListMap { get; set; }

        public MappedState<Hash, AddressList> HelpfulNodeListMap { get; set; }

        /// <summary>
        /// For queries no need to aggregate.
        /// </summary>
        public MappedState<Hash, PlainResult> PlainResultMap { get; set; }

        public MappedState<Address, long> OracleNodesLockedTokenAmountMap { get; set; }

        /// <summary>
        /// From address -> Regiment Association Address -> Amount
        /// </summary>
        public MappedState<Address, Address, long> LockedTokenFromAddressMap { get; set; }

        public BoolState IsChargeFee { get; set; }

        /// <summary>
        /// Task Id -> Query Task
        /// </summary>
        public MappedState<Hash, QueryTask> QueryTaskMap { get; set; }

        public MappedState<Address, bool> PostPayAddressMap { get; set; }
    }
}