using AElf.Contracts.MerkleTreeGeneratorContract;
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
            State.MerkleTreeGeneratorContract.Value = input.MerkleTreeGeneratorContractAddress;

            // Initial MTRecorder Contract, then Bridge Contract will be the Owner of MTRecorder Contract.
            State.MerkleTreeRecorderContract.Initialize.Send(new Empty());

            State.MerkleTreeGeneratorContract.Initialize.Send(new MerkleTreeGeneratorContract.InitializeInput
            {
                Owner = Context.Self
            });
            State.MerkleTreeGeneratorContract.RegisterReceiptMaker.Send(new RegisterReceiptMakerInput
            {
                ReceiptMakerAddress = Context.Self,
                MerkleTreeLeafLimit = input.MerkleTreeLeafLimit == 0 ? MaximalLeafCount : input.MerkleTreeLeafLimit
            });
            return new Empty();
        }
    }
}