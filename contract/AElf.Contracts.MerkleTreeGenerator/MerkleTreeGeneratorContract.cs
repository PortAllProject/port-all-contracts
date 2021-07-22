using System;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.MerkleTreeGeneratorContract
{
    public partial class
        MerkleTreeGeneratorContract : MerkleTreeGeneratorContractContainer.MerkleTreeGeneratorContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.Owner.Value == null, "Already initialized.");
            State.Owner.Value = input.Owner;
            return new Empty();
        }

        public override Empty RegisterReceiptMaker(RegisterReceiptMakerInput input)
        {
            Assert(State.Owner.Value == Context.Sender, "No permission.");
            Assert(State.ReceiptMakerMap[input.ReceiptMakerAddress] == null, "Already registered.");
            State.ReceiptMakerMap[input.ReceiptMakerAddress] = new ReceiptMaker
            {
                ReceiptMakerAddress = input.ReceiptMakerAddress,
                MerkleTreeLeafLimit = input.MerkleTreeLeafLimit
            };
            Context.Fire(new ReceiptMakerRegistered
            {
                ReceiptMakerAddress = input.ReceiptMakerAddress
            });
            return new Empty();
        }

        public override Empty UnRegisterReceiptMaker(Address input)
        {
            Assert(State.Owner.Value == Context.Sender, "No permission.");
            Assert(State.ReceiptMakerMap[input] != null, "Not registered.");
            State.ReceiptMakerMap.Remove(input);
            Context.Fire(new ReceiptMakerUnRegistered
            {
                ReceiptMakerAddress = input
            });
            return new Empty();
        }

        public override GetMerkleTreeOutput GetMerkleTree(GetMerkleTreeInput input)
        {
            Assert(State.ReceiptMakerMap[input.ReceiptMakerAddress] != null, "Receipt maker not registered.");
            var maker = State.ReceiptMakerMap[input.ReceiptMakerAddress];
            var merkleTree = ConstructMerkleTree(maker.ReceiptMakerAddress, input.ExpectedFullTreeIndex,
                maker.MerkleTreeLeafLimit);
            return new GetMerkleTreeOutput
            {
                MerkleTreeRoot = merkleTree.MerkleTreeRoot,
                FirstIndex = merkleTree.FirstLeafIndex,
                LastIndex = merkleTree.LastLeafIndex,
                IsFullTree = merkleTree.IsFullTree
            };
        }

        public override Int64Value GetFullTreeCount(Address input)
        {
            var maker = State.ReceiptMakerMap[input];
            if (maker == null)
            {
                throw new AssertionException("Receipt maker not registered.");
            }

            var receiptCount = GetReceiptCount(input);
            return new Int64Value { Value = receiptCount.Div(maker.MerkleTreeLeafLimit) };
        }

        public override GetReceiptMakerOutput GetReceiptMaker(Address input)
        {
            var maker = State.ReceiptMakerMap[input];
            return new GetReceiptMakerOutput
            {
                ReceiptMakerAddress = maker.ReceiptMakerAddress,
                MerkleTreeLeafLimit = maker.MerkleTreeLeafLimit
            };
        }

        public override MerklePath GetMerklePath(GetMerklePathInput input)
        {
            var maker = State.ReceiptMakerMap[input.ReceiptMaker];
            if (maker == null)
            {
                throw new AssertionException("Receipt maker not registered.");
            }

            var receiptCount = GetReceiptCount(input.ReceiptMaker);
            Assert(receiptCount > 0, "Receipts not found.");
            var firstLeafIndex = input.ReceiptId.Div(maker.MerkleTreeLeafLimit).Mul(maker.MerkleTreeLeafLimit);
            var maxLastLeafIndex = firstLeafIndex.Add(maker.MerkleTreeLeafLimit).Sub(1);
            var lastLeafIndex = Math.Min(maxLastLeafIndex, input.LastLeafIndex);
            Assert(lastLeafIndex >= input.ReceiptId && lastLeafIndex >= firstLeafIndex,
                "Invalid merkle input.");

            var binaryMerkleTree = GenerateMerkleTree(input.ReceiptMaker, firstLeafIndex, lastLeafIndex);
            var index = (int)input.ReceiptId.Sub(firstLeafIndex);
            var path = binaryMerkleTree.GenerateMerklePath(index);
            return path;
        }
    }
}