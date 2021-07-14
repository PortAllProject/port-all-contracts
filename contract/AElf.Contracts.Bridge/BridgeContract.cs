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
            State.ParliamentContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
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
            State.MaximalLeafCount.Value =
                input.MerkleTreeLeafLimit == 0 ? DefaultMaximalLeafCount : input.MerkleTreeLeafLimit;
            State.MerkleTreeGeneratorContract.RegisterReceiptMaker.Send(new RegisterReceiptMakerInput
            {
                ReceiptMakerAddress = Context.Self,
                MerkleTreeLeafLimit = State.MaximalLeafCount.Value
            });
            return new Empty();
        }
    }
}