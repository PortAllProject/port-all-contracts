using System.Linq;
using AElf.Contracts.MerkleTreeGeneratorContract;
using AElf.Contracts.ReceiptMakerContract;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using MTRecorder;

namespace AElf.Contracts.Bridge
{
    public partial class BridgeContract
    {
        public override Empty RecordReceiptHash(CallbackInput input)
        {
            Assert(Context.Sender == State.OracleContract.Value, "No permission.");
            var queryResult = new StringValue();
            queryResult.MergeFrom(input.Result);
            var receiptHashMap = JsonParser.Default.Parse<ReceiptHashMap>(queryResult.Value);
            foreach (var (receiptId, receiptHash) in receiptHashMap.Value)
            {
                State.RecorderReceiptHashMap[receiptHashMap.RecorderId][receiptId] = Hash.LoadFromHex(receiptHash);
            }

            State.ReceiptCountMap[receiptHashMap.RecorderId] = receiptHashMap.Value.Last().Key.Add(1);

            Context.SendInline(Context.Self, nameof(UpdateMerkleTree), new UpdateMerkleTreeInput
            {
                RecorderId = receiptHashMap.RecorderId,
                RegimentAddress = input.OracleNodes.First()
            }.ToByteString());

            return new Empty();
        }

        public override Empty UpdateMerkleTree(UpdateMerkleTreeInput input)
        {
            Assert(Context.Sender == Context.Self, "No permission.");

            var getMerkleTreeOutput = State.MerkleTreeGeneratorContract.GetMerkleTree.Call(
                new MerkleTreeGeneratorContract.GetMerkleTreeInput
                {
                    ReceiptMakerAddress = Context.Self,
                    ExpectedFullTreeIndex =
                        State.ReceiptCountMap[input.RecorderId].Sub(1).Div(State.MaximalLeafCount.Value),
                    RecorderId = input.RecorderId
                });

            var recordMerkleTreeInput = new RecordMerkleTreeInput
            {
                RecorderId = input.RecorderId,
                MerkleTreeRoot = getMerkleTreeOutput.MerkleTreeRoot,
                LastLeafIndex = getMerkleTreeOutput.LastIndex
            };

            // Make sure certain oracle nodes (regiment) is aiming at record for this recorder id.
            Assert(State.RecorderIdToRegimentMap[recordMerkleTreeInput.RecorderId] == input.RegimentAddress,
                $"Recorder id does not belong to regiment {input.RegimentAddress}.");

            State.MerkleTreeRecorderContract.RecordMerkleTree.Send(recordMerkleTreeInput);
            return new Empty();
        }

        public override Int64Value GetReceiptCount(Int64Value input)
        {
            return new Int64Value {Value = State.ReceiptCountMap[input.Value]};
        }

        public override Hash GetReceiptHash(GetReceiptHashInput input)
        {
            return State.RecorderReceiptHashMap[input.RecorderId][input.ReceiptId] ??
                   State.ReceiptHashMap[input.ReceiptId];
        }

        public override GetReceiptHashListOutput GetReceiptHashList(GetReceiptHashListInput input)
        {
            var output = new GetReceiptHashListOutput();
            for (var i = input.FirstLeafIndex; i <= input.LastLeafIndex; i++)
            {
                var receiptHash = GetReceiptHash(new GetReceiptHashInput
                {
                    RecorderId = input.RecorderId,
                    ReceiptId = i
                });
                Assert(receiptHash != null, $"Receipt hash of {i} is null.");
                output.ReceiptHashList.Add(receiptHash);
            }

            return output;
        }

        public override Empty ChangeMaximalLeafCount(Int32Value input)
        {
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }

            var parliamentDefaultAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            Assert(Context.Sender == parliamentDefaultAddress, "No permission.");
            State.MaximalLeafCount.Value = input.Value;
            return new Empty();
        }
    }
}