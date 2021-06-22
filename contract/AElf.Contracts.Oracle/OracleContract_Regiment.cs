using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
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
                InitialMemberList = {input.InitialMemberList},
                IsApproveToJoin = input.IsApproveToJoin,
                Manager = Context.Sender
            });
            return new Empty();
        }

        public override Empty JoinRegiment(JoinRegimentInput input)
        {
            State.RegimentContract.JoinRegiment.Send(new Regiment.JoinRegimentInput
            {
                RegimentAddress = input.RegimentAddress,
                NewMemberAddress = input.NewMemberAddress,
                OriginSenderAddress = Context.Sender
            });
            return new Empty();
        }

        public override Empty LeaveRegiment(LeaveRegimentInput input)
        {
            Assert(input.LeaveMemberAddress == Context.Sender, "No permission.");
            State.RegimentContract.LeaveRegiment.Send(new Regiment.LeaveRegimentInput
            {
                RegimentAddress = input.RegimentAddress,
                LeaveMemberAddress = input.LeaveMemberAddress,
                OriginSenderAddress = Context.Sender
            });
            return new Empty();
        }

        public override Empty AddRegimentMember(AddRegimentMemberInput input)
        {
            State.RegimentContract.AddRegimentMember.Send(new Regiment.AddRegimentMemberInput
            {
                RegimentAddress = input.RegimentAddress,
                NewMemberAddress = input.NewMemberAddress,
                OriginSenderAddress = Context.Sender
            });
            return new Empty();
        }

        public override Empty DeleteRegimentMember(DeleteRegimentMemberInput input)
        {
            State.RegimentContract.DeleteRegimentMember.Send(new Regiment.DeleteRegimentMemberInput
            {
                RegimentAddress = input.RegimentAddress,
                DeleteMemberAddress = input.DeleteMemberAddress,
                OriginSenderAddress = Context.Sender
            });
            return new Empty();
        }

        public override Empty TransferRegimentOwnership(TransferRegimentOwnershipInput input)
        {
            State.RegimentContract.TransferRegimentOwnership.Send(new Regiment.TransferRegimentOwnershipInput
            {
                RegimentAddress = input.RegimentAddress,
                NewManagerAddress = input.NewManagerAddress,
                OriginSenderAddress = Context.Sender
            });
            return new Empty();
        }

        public override Empty AddAdmins(AddAdminsInput input)
        {
            State.RegimentContract.AddAdmins.Send(new Regiment.AddAdminsInput
            {
                RegimentAddress = input.RegimentAddress,
                NewAdmins = {input.NewAdmins},
                OriginSenderAddress = Context.Sender
            });
            return new Empty();
        }

        public override Empty DeleteAdmins(DeleteAdminsInput input)
        {
            State.RegimentContract.DeleteAdmins.Send(new Regiment.DeleteAdminsInput
            {
                RegimentAddress = input.RegimentAddress,
                DeleteAdmins = {input.DeleteAdmins},
                OriginSenderAddress = Context.Sender
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