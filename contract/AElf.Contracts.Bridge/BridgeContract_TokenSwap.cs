using System;
using System.Linq;
using AElf.Contracts.MerkleTreeGeneratorContract;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using MTRecorder;

namespace AElf.Contracts.Bridge
{
    public partial class BridgeContract
    {
        public override Hash CreateSwap(CreateSwapInput input)
        {
            Assert(input.RegimentAddress != null, "Regiment address cannot be null.");
            Assert(State.MerkleTreeRecorderContract.Value != null, "Not initialized.");
            var swapId = HashHelper.ConcatAndCompute(Context.TransactionId, HashHelper.ComputeFrom(input));
            Assert(State.SwapInfo[swapId] == null, "Swap already created.");
            var regimentManager = State.RegimentContract.GetRegimentInfo.Call(input.RegimentAddress).Manager;
            Assert(Context.Sender == regimentManager, "Only regiment manager can create swap.");

            State.MerkleTreeRecorderContract.CreateRecorder.Send(new Recorder
            {
                Admin = Context.Self,
                MaximalLeafCount = State.MaximalLeafCount.Value
            });

            var recorderId = State.MerkleTreeRecorderContract.GetRecorderCount.Call(new Empty()).Value;
            var swapInfo = new SwapInfo
            {
                SwapId = swapId,
                OriginTokenNumericBigEndian = input.OriginTokenNumericBigEndian,
                OriginTokenSizeInByte = input.OriginTokenSizeInByte,
                RegimentAddress = input.RegimentAddress,
                RecorderId = recorderId
            };

            foreach (var swapTargetToken in input.SwapTargetTokenList)
            {
                AssertSwapTargetToken(swapTargetToken.TargetTokenSymbol);
                var swapPair = new SwapPair
                {
                    SwapId = swapId,
                    OriginTokenSizeInByte = input.OriginTokenSizeInByte,
                    OriginTokenNumericBigEndian = input.OriginTokenNumericBigEndian,
                    TargetTokenSymbol = swapTargetToken.TargetTokenSymbol,
                    SwapRatio = swapTargetToken.SwapRatio,
                    DepositAmount = swapTargetToken.DepositAmount
                };
                AssertValidSwapPair(swapPair);
                var pairId =
                    HashHelper.ConcatAndCompute(swapId, HashHelper.ComputeFrom(swapTargetToken.TargetTokenSymbol));
                swapInfo.SwapTargetTokenMap.Add(swapTargetToken.TargetTokenSymbol, pairId);
                State.SwapPairs[pairId] = swapPair;
                TransferDepositFrom(swapTargetToken.TargetTokenSymbol, swapTargetToken.DepositAmount, Context.Sender);
            }

            State.RecorderIdToRegimentMap[recorderId] = input.RegimentAddress;

            State.SwapInfo[swapId] = swapInfo;
            Context.Fire(new SwapPairAdded {SwapId = swapId});
            return swapId;
        }

        public override Empty SwapToken(SwapTokenInput input)
        {
            var receiverAddress = Context.Sender;
            var swapInfo = GetTokenSwapInfo(input.SwapId);
            ValidateSwapTokenInput(input);
            Assert(TryGetOriginTokenAmount(input.OriginAmount, out var amount) && amount > 0,
                "Invalid token swap input.");
            var leafHash = ComputeLeafHash(amount, swapInfo, receiverAddress, input.ReceiptId);

            var lastRecordedLeafIndex = State.MerkleTreeRecorderContract.GetLastRecordedLeafIndex.Call(
                new RecorderIdInput
                {
                    RecorderId = swapInfo.RecorderId
                }).Value;

            // To locate the tree of specific receipt id.
            var firstLeafIndex = input.ReceiptId.Div(State.MaximalLeafCount.Value).Mul(State.MaximalLeafCount.Value);
            var maxLastLeafIndex = firstLeafIndex.Add(State.MaximalLeafCount.Value).Sub(1);
            var lastLeafIndex = Math.Min(maxLastLeafIndex, lastRecordedLeafIndex);
            var merklePath = State.MerkleTreeGeneratorContract.GetMerklePath.Call(new GetMerklePathInput
            {
                ReceiptMaker = Context.Self,
                ReceiptId = input.ReceiptId,
                FirstLeafIndex = firstLeafIndex,
                LastLeafIndex = lastLeafIndex,
                RecorderId = swapInfo.RecorderId
            });

            Assert(State.MerkleTreeRecorderContract.MerkleProof.Call(new MerkleProofInput
            {
                LastLeafIndex = lastLeafIndex,
                LeafNode = leafHash,
                MerklePath = merklePath,
                RecorderId = swapInfo.RecorderId
            }).Value, "Merkle proof failed.");

            var swapAmounts = new SwapAmounts
            {
                Receiver = receiverAddress
            };
            foreach (var (symbol, pairId) in swapInfo.SwapTargetTokenMap)
            {
                var swapPair = GetTokenSwapPair(pairId);
                var targetTokenAmount = GetTargetTokenAmount(amount, swapPair.SwapRatio);
                Assert(targetTokenAmount <= swapPair.DepositAmount, "Deposit not enough.");

                // Update swap pair and ledger
                swapPair.SwappedAmount = swapPair.SwappedAmount.Add(targetTokenAmount);
                swapPair.SwappedTimes = swapPair.SwappedTimes.Add(1);
                swapPair.DepositAmount = swapPair.DepositAmount.Sub(targetTokenAmount);

                AssertValidSwapPair(swapPair);
                State.SwapPairs[input.SwapId] = swapPair;

                // Do transfer
                TransferToken(swapPair.TargetTokenSymbol, targetTokenAmount, receiverAddress);
                Context.Fire(new TokenSwapped
                {
                    Amount = targetTokenAmount,
                    Address = receiverAddress,
                    Symbol = swapPair.TargetTokenSymbol
                });

                swapAmounts.ReceivedAmounts[symbol] = targetTokenAmount;
            }

            State.Ledger[input.SwapId][input.ReceiptId] = swapAmounts;
            State.RecorderReceiptInfoMap[input.SwapId][input.ReceiptId] = new ReceiptInfo
            {
                ReceiptId = input.ReceiptId,
                ReceivingTime = Context.CurrentBlockTime,
                ReceivingTxId = Context.TransactionId,
                Amount = swapAmounts.ReceivedAmounts[swapInfo.SwapTargetTokenMap.Keys.First()],
                AmountMap = {swapAmounts.ReceivedAmounts}
            };

            var swappedReceiptIdList =
                State.SwappedReceiptIdListMap[input.SwapId][receiverAddress] ?? new ReceiptIdList();
            swappedReceiptIdList.Value.Add(input.ReceiptId);
            State.SwappedReceiptIdListMap[input.SwapId][receiverAddress] = swappedReceiptIdList;
            return new Empty();
        }

        private ReceiptInfo GetReceiptInfo(Hash swapId, long receiptId)
        {
            return State.RecorderReceiptInfoMap[swapId][receiptId] ?? State.ReceiptInfoMap[receiptId];
        }

        public override Empty ChangeSwapRatio(ChangeSwapRatioInput input)
        {
            var swapInfo = GetTokenSwapInfo(input.SwapId);
            var regimentManager = State.RegimentContract.GetRegimentInfo.Call(swapInfo.RegimentAddress).Manager;
            Assert(Context.Sender == regimentManager, "No permission.");
            Assert(swapInfo.SwapTargetTokenMap.TryGetValue(input.TargetTokenSymbol, out var pairId),
                "Target token not registered.");
            var swapPair = GetTokenSwapPair(pairId);
            swapPair.SwapRatio = input.SwapRatio;
            AssertValidSwapPair(swapPair);
            State.SwapPairs[pairId] = swapPair;
            Context.Fire(new SwapRatioChanged
            {
                SwapId = input.SwapId,
                NewSwapRatio = input.SwapRatio,
                TargetTokenSymbol = input.TargetTokenSymbol
            });
            return new Empty();
        }

        public override SwapInfo GetSwapInfo(Hash input)
        {
            return State.SwapInfo[input];
        }

        public override SwapPair GetSwapPair(GetSwapPairInput input)
        {
            var swapInfo = GetTokenSwapInfo(input.SwapId);
            Assert(swapInfo.SwapTargetTokenMap.TryGetValue(input.TargetTokenSymbol, out var pairId),
                "Target token not registered.");
            var swapPair = GetTokenSwapPair(pairId);
            return swapPair;
        }

        public override Empty Deposit(DepositInput input)
        {
            var swapInfo = GetTokenSwapInfo(input.SwapId);
            var regimentManager = State.RegimentContract.GetRegimentInfo.Call(swapInfo.RegimentAddress).Manager;
            Assert(Context.Sender == regimentManager, "No permission.");
            var swapPairId = swapInfo.SwapTargetTokenMap[input.TargetTokenSymbol];
            var swapPair = GetTokenSwapPair(swapPairId);
            swapPair.DepositAmount = swapPair.DepositAmount.Add(input.Amount);
            AssertValidSwapPair(swapPair);
            State.SwapPairs[swapPairId] = swapPair;
            TransferDepositFrom(swapPair.TargetTokenSymbol, input.Amount, Context.Sender);
            return new Empty();
        }

        public override Empty Withdraw(WithdrawInput input)
        {
            var swapInfo = GetTokenSwapInfo(input.SwapId);
            var regimentManager = State.RegimentContract.GetRegimentInfo.Call(swapInfo.RegimentAddress).Manager;
            Assert(Context.Sender == regimentManager, "No permission.");
            var swapPairId = swapInfo.SwapTargetTokenMap[input.TargetTokenSymbol];
            var swapPair = GetTokenSwapPair(swapPairId);
            Assert(swapPair.DepositAmount >= input.Amount, "Deposits not enough.");
            swapPair.DepositAmount = swapPair.DepositAmount.Sub(input.Amount);
            AssertValidSwapPair(swapPair);
            State.SwapPairs[swapPairId] = swapPair;
            WithdrawDepositTo(swapPair.TargetTokenSymbol, input.Amount, Context.Sender);
            return new Empty();
        }

        public override SwapAmounts GetSwapAmounts(GetSwapAmountsInput input)
        {
            return State.Ledger[input.SwapId][input.ReceiptId];
        }

        public override Address GetRegimentAddressByRecorderId(Int64Value input)
        {
            return State.RecorderIdToRegimentMap[input.Value];
        }

        public override ReceiptIdList GetSwappedReceiptIdList(GetSwappedReceiptIdListInput input)
        {
            return State.SwappedReceiptIdListMap[input.SwapId][input.ReceiverAddress];
        }

        public override ReceiptInfoList GetSwappedReceiptInfoList(GetSwappedReceiptInfoListInput input)
        {
            var receiptInfoList = new ReceiptInfoList();
            var receiptIdList = State.SwappedReceiptIdListMap[input.SwapId][input.ReceivingAddress];
            if (receiptIdList == null)
            {
                return receiptInfoList;
            }

            foreach (var receiptId in receiptIdList.Value)
            {
                var receiptInfo = GetReceiptInfo(input.SwapId, receiptId);
                if (receiptInfo != null)
                {
                    receiptInfoList.Value.Add(receiptInfo);
                }
                else
                {
                    var swapAmounts = State.Ledger[input.SwapId][receiptId];
                    var amount = swapAmounts?.ReceivedAmounts[Context.Variables.NativeSymbol] ?? 0;
                    receiptInfoList.Value.Add(new ReceiptInfo
                    {
                        ReceiptId = receiptId,
                        Amount = amount
                    });
                }
            }

            return receiptInfoList;
        }
    }
}