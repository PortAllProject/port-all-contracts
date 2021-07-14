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
        public Int64State ReceiptCount { get; set; }
        public MappedState<long, Hash> ReceiptHashMap { get; set; }
    }
}