using AElf.Sdk.CSharp.State;
using AElf.Types;
using MTRecorder;

namespace AElf.Contracts.Bridge
{
    public partial class BridgeContractState : ContractState
    {
        public SingletonState<Address> Controller { get; set; }
        public MappedState<string, BridgeTokenInfo> BridgeTokenInfoMap { get; set; }

        public MappedState<Hash, SwapInfo> SwapInfo { get; set; }
        public MappedState<Hash, SwapPair> SwapPairs { get; set; }
        public MappedState<Hash, Hash, SwapAmounts> Ledger { get; set; }

        internal MerkleTreeRecorderContractContainer.MerkleTreeRecorderContractReferenceState MerkleTreeRecorderContract
        {
            get;
            set;
        }
    }
}