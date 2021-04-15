using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContract
    {
        public override Empty RegisterOracleNode(RegisterOracleNodeInput input)
        {
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = input.LockAmount,
                Symbol = TokenSymbol
            });
            var currentAmount = State.OracleNodesLockedTokenAmountMap[input.Address];
            var newAmount = currentAmount.Add(input.LockAmount);
            State.OracleNodesLockedTokenAmountMap[input.Address] = newAmount;
            Context.Fire(new OracleNodeRegistered
            {
                Address = input.Address,
                LockedAmount = newAmount
            });
            return new Empty();
        }
    }
}