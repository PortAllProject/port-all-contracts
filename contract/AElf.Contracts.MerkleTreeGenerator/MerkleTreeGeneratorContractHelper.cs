using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.ReceiptMakerContract;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MerkleTreeGeneratorContract
{
    public partial class MerkleTreeGeneratorContract
    {
        private MerkleTree ConstructMerkleTree(Address receiptMaker, long expectFullTreeIndex, int leafCountLimit,
            long recorderId)
        {
            var receiptCount = GetReceiptCount(receiptMaker, recorderId);
            var treeCount = receiptCount % leafCountLimit == 0
                ? receiptCount.Div(leafCountLimit)
                : receiptCount.Div(leafCountLimit).Add(1);

            Assert(expectFullTreeIndex >= 0 && receiptCount > 0 && treeCount > expectFullTreeIndex,
                "Unable to generate this merkle tree.");

            var isFullTree = (expectFullTreeIndex + 1).Mul(leafCountLimit) <= receiptCount;
            var firstLeafIndex = expectFullTreeIndex.Mul(leafCountLimit);
            var lastLeafIndex = isFullTree
                ? expectFullTreeIndex.Add(1).Mul(leafCountLimit).Sub(1)
                : receiptCount.Sub(1);
            var binaryMerkleTree = GenerateMerkleTree(receiptMaker, firstLeafIndex, lastLeafIndex, recorderId);

            return new MerkleTree
            {
                FirstLeafIndex = firstLeafIndex,
                LastLeafIndex = lastLeafIndex,
                MerkleTreeRoot = binaryMerkleTree.Root,
                IsFullTree = isFullTree
            };
        }

        private BinaryMerkleTree GenerateMerkleTree(Address receiptMaker, long firstLeafIndex, long lastLeafIndex,
            long recorderId)
        {
            var receiptHashList = GetReceiptHashList(receiptMaker, firstLeafIndex, lastLeafIndex, recorderId);
            var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(receiptHashList);
            return binaryMerkleTree;
        }

        private List<Hash> GetReceiptHashList(Address receiptMaker, long firstLeafIndex, long lastLeafIndex,
            long recorderId)
        {
            var receiptHashList = Context.Call<GetReceiptHashListOutput>(receiptMaker,
                nameof(ReceiptMakerContractContainer.ReceiptMakerContractReferenceState.GetReceiptHashList),
                new GetReceiptHashListInput
                {
                    FirstLeafIndex = firstLeafIndex,
                    LastLeafIndex = lastLeafIndex,
                    RecorderId = recorderId
                });

            return receiptHashList.ReceiptHashList.ToList();
        }

        private long GetReceiptCount(Address receiptMaker, long recorderId)
        {
            var receiptCount = Context.Call<Int64Value>(receiptMaker,
                nameof(ReceiptMakerContractContainer.ReceiptMakerContractReferenceState.GetReceiptCount),
                new Int64Value {Value = recorderId});
            return receiptCount.Value;
        }
    }
}