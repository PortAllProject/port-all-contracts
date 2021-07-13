using System.Linq;
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
        public override Hash CreateSwap(CreateSwapInput input)
        {
            Assert(State.MerkleTreeRecorderContract.Value != null, "Not initialized.");
            var swapId = HashHelper.ConcatAndCompute(Context.TransactionId, HashHelper.ComputeFrom(input));
            Assert(State.SwapInfo[swapId] == null, "Swap already created.");
            var regimentManager = State.RegimentContract.GetRegimentInfo.Call(input.RegimentAddress).Manager;
            Assert(Context.Sender == regimentManager, "Only regiment manager can create swap.");

            State.MerkleTreeRecorderContract.CreateRecorder.Send(new Recorder
            {
                Admin = Context.Self,
                MaximalLeafCount = MaximalLeafCount
            });

            var swapInfo = new SwapInfo
            {
                SwapId = swapId,
                OriginTokenNumericBigEndian = input.OriginTokenNumericBigEndian,
                OriginTokenSizeInByte = input.OriginTokenSizeInByte,
                RegimentAddress = input.RegimentAddress,
                RecorderId = State.MerkleTreeRecorderContract.GetRecorderCount.Call(new Empty()).Value.Add(1)
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
            var leafHash = ComputeLeafHash(amount, input.UniqueId, swapInfo, receiverAddress);
            Assert(State.MerkleTreeRecorderContract.MerkleProof.Call(new MerkleProofInput
            {
                LastLeafIndex = input.LastLeafIndex,
                LeafNode = leafHash,
                MerklePath = input.MerklePath,
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

            State.Ledger[input.SwapId][input.UniqueId] = swapAmounts;

            return new Empty();
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
            var swapInfo = State.SwapInfo[input];
            return swapInfo;
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

        public override Empty RecordMerkleTree(CallbackInput input)
        {
            Assert(Context.Sender == State.OracleContract.Value, "No permission.");
            var queryResult = new StringValue();
            queryResult.MergeFrom(input.Result);
            var recordMerkleTreeInput = JsonParser.Default.Parse<RecordMerkleTreeInput>(queryResult.Value);
            // Make sure certain oracle nodes (regiment) is aiming at record for this recorder id.
            Assert(State.RecorderIdToRegimentMap[recordMerkleTreeInput.RecorderId] == input.OracleNodes.First(),
                "Incorrect recorder id.");
            State.MerkleTreeRecorderContract.RecordMerkleTree.Send(recordMerkleTreeInput);
            return new Empty();
        }

        public override SwapAmounts GetSwapAmounts(GetSwapAmountsInput input)
        {
            return State.Ledger[input.SwapId][input.UniqueId];
        }
    }
}