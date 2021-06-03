using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS3;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContract
    {
        public override Empty CreateRegiment(CreateRegimentInput input)
        {
            // Need to pay.

            State.RegimentContract.CreateRegiment.Send(new Regiment.CreateRegimentInput
            {
                InitialMemberList = { input.InitialMemberList},
                IsApproveToJoin = input.IsApproveToJoin,
                Manager = Context.Sender
            });

            return new Empty();
        }

        public override Empty LockTokens(LockTokensInput input)
        {
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = input.LockAmount,
                Symbol = TokenSymbol
            });
            var currentAmount = State.OracleNodesLockedTokenAmountMap[input.OracleNodeAddress];
            var newAmount = currentAmount.Add(input.LockAmount);
            State.OracleNodesLockedTokenAmountMap[input.OracleNodeAddress] = newAmount;
            State.LockedTokenFromAddressMap[Context.Sender][input.OracleNodeAddress] =
                State.LockedTokenFromAddressMap[Context.Sender][input.OracleNodeAddress].Add(input.LockAmount);
            Context.Fire(new TokenLocked
            {
                OracleNodeAddress = input.OracleNodeAddress,
                FromAddress = Context.Sender,
                LockedAmount = newAmount
            });
            return new Empty();
        }

        public override Empty UnlockTokens(UnlockTokensInput input)
        {
            var actualLockedAmount = State.LockedTokenFromAddressMap[Context.Sender][input.OracleNodeAddress];
            Assert(actualLockedAmount >= input.WithdrawAmount, "Invalid withdraw amount.");
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = Context.Sender,
                Symbol = TokenSymbol,
                Amount = input.WithdrawAmount
            });
            State.OracleNodesLockedTokenAmountMap[input.OracleNodeAddress] = State
                .OracleNodesLockedTokenAmountMap[input.OracleNodeAddress].Sub(input.WithdrawAmount);
            State.LockedTokenFromAddressMap[Context.Sender][input.OracleNodeAddress] =
                State.LockedTokenFromAddressMap[Context.Sender][input.OracleNodeAddress].Sub(input.WithdrawAmount);
            return new Empty();
        }
    }
}