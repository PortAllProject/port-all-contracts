using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Bridge
{
    public partial class BridgeContractState : ContractState
    {
        public MappedState<Hash, SwapInfo> SwapInfo { get; set; }
        public MappedState<Hash, SwapPair> SwapPairs { get; set; }
        public MappedState<Hash, long, SwapAmounts> Ledger { get; set; }
        public MappedState<long, Address> RecorderIdToRegimentMap { get; set; }

        /// <summary>
        /// Recorder Id -> Receipt Count
        /// </summary>
        public MappedState<long, long> ReceiptCountMap { get; set; }

        /// <summary>
        /// Not use
        /// </summary>
        public MappedState<long, Hash> ReceiptHashMap { get; set; }

        /// <summary>
        /// Recorder Id -> Receipt Id -> Receipt Hash
        /// </summary>
        public MappedState<long, long, Hash> RecorderReceiptHashMap { get; set; }

        public Int32State MaximalLeafCount { get; set; }

        /// <summary>
        /// Swap Id -> Receiver Address -> Swapped Receipt Id List
        /// </summary>
        public MappedState<Hash, Address, ReceiptIdList> SwappedReceiptIdListMap { get; set; }

        /// <summary>
        /// Not use.
        /// </summary>
        public MappedState<long, ReceiptInfo> ReceiptInfoMap { get; set; }

        /// <summary>
        /// Swap Id -> Receipt Id -> Receipt Hash
        /// </summary>
        public MappedState<Hash, long, ReceiptInfo> RecorderReceiptInfoMap { get; set; }
    }
}