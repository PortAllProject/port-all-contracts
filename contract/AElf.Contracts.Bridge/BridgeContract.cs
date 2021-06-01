using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Bridge
{
    public partial class BridgeContract : BridgeContractContainer.BridgeContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            return new Empty();
        }
    }
}