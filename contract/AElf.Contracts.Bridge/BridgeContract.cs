using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Bridge
{
    public partial class BridgeContract : BridgeContractContainer.BridgeContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.MerkleTreeRecorderContract.Value == null, "Already initialized.");
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.OracleContract.Value = input.OracleContractAddress;
            State.MerkleTreeRecorderContract.Value = input.MerkleTreeRecorderContractAddress;
            State.RegimentContract.Value = input.RegimentContractAddress;

            // Initial MTRecorder Contract, then Bridge Contract will be the Owner of MTRecorder Contract.
            State.MerkleTreeRecorderContract.Initialize.Send(new Empty());
            return new Empty();
        }
    }
}