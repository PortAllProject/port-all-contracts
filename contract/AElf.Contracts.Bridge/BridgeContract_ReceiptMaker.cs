using System.Linq;
using AElf.Contracts.MerkleTreeGeneratorContract;
using AElf.Contracts.ReceiptMakerContract;
using AElf.CSharp.Core;
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
                State.ReceiptHashMap[receiptId] = receiptHash;
            }

            State.ReceiptCount.Value = State.ReceiptCount.Value.Add(receiptHashMap.Value.Count);

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
                    ExpectedFullTreeIndex = State.ReceiptCount.Value.Div(MaximalLeafCount)
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

        public override Int64Value GetReceiptCount(Empty input)
        {
            return new Int64Value {Value = State.ReceiptCount.Value};
        }

        public override Hash GetReceiptHash(Int64Value input)
        {
            return State.ReceiptHashMap[input.Value];
        }

        public override GetReceiptHashListOutput GetReceiptHashList(GetReceiptHashListInput input)
        {
            var output = new GetReceiptHashListOutput();
            for (var i = input.FirstLeafIndex; i <= input.LastLeafIndex; i++)
            {
                output.ReceiptHashList.Add(State.ReceiptHashMap[i]);
            }

            return output;
        }
    }
}