using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Bridge
{
    public partial class BridgeContract
    {
        public override Empty Transfer(TransferInput input)
        {
            return new Empty();
        }
    }
}