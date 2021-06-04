using System;
using System.Linq;
using AElf.Contracts.Oracle;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChainToken
{
    public partial class CrossChainTokenContract : CrossChainTokenContractContainer.CrossChainTokenContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            State.OracleContract.Value = input.OracleContractAddress;
            State.ParliamentContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

            State.OracleTokenSymbol.Value = State.OracleContract.GetOracleTokenSymbol.Call(new Empty()).Value;
            return new Empty();
        }

        public override Empty Transfer(TransferInput input)
        {
            DoTransfer(Context.Sender, input.To, GetTokenFullName(input.Symbol, input.Field), input.Amount, input.Memo);
            return new Empty();
        }

        public override Empty TransferFrom(TransferFromInput input)
        {
            var tokenFullName = GetTokenFullName(input.Symbol, input.Field);
            // First check allowance.
            var allowance = State.Allowances[input.From][Context.Sender][tokenFullName];
            if (allowance < input.Amount)
            {
                Assert(false,
                    $"[TransferFrom]Insufficient allowance. Token: {tokenFullName}; {allowance}/{input.Amount}.\n" +
                    $"From:{input.From}\tSpender:{Context.Sender}\tTo:{input.To}");
            }

            DoTransfer(input.From, input.To, tokenFullName, input.Amount, input.Memo);
            State.Allowances[input.From][Context.Sender][tokenFullName] = allowance.Sub(input.Amount);
            return new Empty();
        }

        public override Empty Approve(ApproveInput input)
        {
            var tokenFullName = GetTokenFullName(input.Symbol, input.Field);
            State.Allowances[Context.Sender][input.Spender][tokenFullName] =
                State.Allowances[Context.Sender][input.Spender][tokenFullName].Add(input.Amount);
            Context.Fire(new Approved
            {
                Owner = Context.Sender,
                Spender = input.Spender,
                TokenFullName = tokenFullName,
                Amount = input.Amount
            });
            return new Empty();
        }

        public override Empty UnApprove(UnApproveInput input)
        {
            var tokenFullName = GetTokenFullName(input.Symbol, input.Field);
            var oldAllowance = State.Allowances[Context.Sender][input.Spender][tokenFullName];
            var amountOrAll = Math.Min(input.Amount, oldAllowance);
            State.Allowances[Context.Sender][input.Spender][tokenFullName] = oldAllowance.Sub(amountOrAll);
            Context.Fire(new UnApproved
            {
                Owner = Context.Sender,
                Spender = input.Spender,
                TokenFullName = tokenFullName,
                Amount = amountOrAll
            });
            return new Empty();
        }

        public override Empty CrossChainTransfer(CrossChainTransferInput input)
        {
            return new Empty();
        }

        public override Empty ClaimCrossChainTokens(ClaimCrossChainTokensInput input)
        {
            Assert(!State.CrossChainTransferFinishedMap[input.EthereumTransactionId],
                $"{input.EthereumTransactionId} already transferred.");
            State.TokenContract.TransferFrom.Send(new MultiToken.TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = CrossChainTokenSymbol,
                Amount = Fee
            });
            State.TokenContract.TransferFrom.Send(new MultiToken.TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = State.OracleTokenSymbol.Value,
                Amount = OraclePayment
            });

            State.OracleContract.Query.Send(new QueryInput
            {
                Payment = OraclePayment,
                AggregateThreshold = 17,
                DesignatedNodeList = new AddressList {Value = {State.ParliamentContract.Value}},
                AggregatorContractAddress = Context.Self,
                QueryInfo = new QueryInfo
                {
                    Title = GetQueryUrl(input.EthereumTransactionId, input.FromChainField),
                    // TODO: Fill
                    Options = { }
                },
                CallbackInfo = new CallbackInfo
                {
                    ContractAddress = Context.Self,
                    MethodName = nameof(CrossChainConfirmToken)
                }
            });

            State.CrossChainTransferFinishedMap[input.EthereumTransactionId] = true;

            Context.Fire(new CrossChainTokenClaimed
            {
                Sender = Context.Sender,
                EthereumTransactionId = input.EthereumTransactionId,
                FromChainField = input.FromChainField
            });

            return new Empty();
        }

        public override Empty CrossChainConfirmToken(CallbackInput input)
        {
            Assert(
                Context.Sender == State.OracleContract.Value &&
                input.OracleNodes.First() == State.ParliamentContract.Value, "No permission.");
            var tokenConfirmInfo = new CrossChainTokenConfirmInfo();
            tokenConfirmInfo.MergeFrom(input.Result);
            var tokenFullName = GetTokenFullName(tokenConfirmInfo.Symbol, tokenConfirmInfo.Field);
            foreach (var receiveInfo in tokenConfirmInfo.TokenReceiveInfos.Value)
            {
                State.Balances[receiveInfo.Address][tokenFullName] =
                    State.Balances[receiveInfo.Address][tokenFullName].Add(receiveInfo.Amount);
                Context.Fire(new CrossChainTokenConfirmed
                {
                    TokenFullName = tokenFullName,
                    Receiver = receiveInfo.Address,
                    Amount = receiveInfo.Amount
                });
            }

            return new Empty();
        }

        public override Empty RegisterMerkleRoot(RegisterMerkleRootInput input)
        {
            return new Empty();
        }
    }
}