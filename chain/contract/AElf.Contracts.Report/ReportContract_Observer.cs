using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Standards.ACS3;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override Empty ApplyObserver(Empty input)
        {
            Assert(GetSenderVirtualAddressBalance(State.ObserverMortgageTokenSymbol.Value) == 0,
                "Sender already applied.");
            TransferTokenToSenderVirtualAddress(State.ObserverMortgageTokenSymbol.Value, State.ApplyObserverFee.Value);
            return new Empty();
        }

        public override Empty QuitObserver(Empty input)
        {
            var currentAmount = GetSenderVirtualAddressBalance(State.ObserverMortgageTokenSymbol.Value);
            Assert(currentAmount > 0, "Sender is not an observer.");
            Context.SendVirtualInline(HashHelper.ComputeFrom(Context.Sender), State.TokenContract.Value,
                nameof(State.TokenContract.Transfer), new TransferInput
                {
                    To = Context.Sender,
                    Symbol = State.ObserverMortgageTokenSymbol.Value,
                    Amount = currentAmount
                }.ToByteString());
            State.ObserverMortgagedTokensMap[Context.Sender] = 0;
            return new Empty();
        }

        public override Empty MortgageTokens(Int64Value input)
        {
            // Maybe transfer some tokens as fees.

            TransferTokenToSenderVirtualAddress(State.ObserverMortgageTokenSymbol.Value, input.Value);
            return new Empty();
        }

        public override Empty WithdrawTokens(Int64Value input)
        {
            TransferTokenFromSenderVirtualAddress(State.ObserverMortgageTokenSymbol.Value, input.Value);
            return new Empty();
        }

        public override Empty ProposeAdjustApplyObserverFee(Int64Value input)
        {
            // Sender mortgaged enough tokens.
            Assert(
                GetSenderVirtualAddressBalance(State.ObserverMortgageTokenSymbol.Value) >=
                State.ApplyObserverFee.Value.Mul(10), "No permission.");
            State.ParliamentContract.CreateProposal.Send(new CreateProposalInput
            {
                ToAddress = Context.Self,
                ContractMethodName = nameof(AdjustApplyObserverFee),
                Params = input.ToByteString(),
                ExpiredTime = Context.CurrentBlockTime.AddDays(1),
                OrganizationAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty())
            });
            return new Empty();
        }

        public override Empty AdjustApplyObserverFee(Int64Value input)
        {
            Assert(Context.Sender == State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty()),
                "No permission.");
            State.ApplyObserverFee.Value = input.Value;
            return new Empty();
        }

        private void TransferTokenToSenderVirtualAddress(string symbol, long amount)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(Context.Sender));
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = virtualAddress,
                Symbol = symbol,
                Amount = amount
            });
            var currentAmount = GetSenderVirtualAddressBalance(symbol);
            State.ObserverMortgagedTokensMap[Context.Sender] = currentAmount.Add(amount);
        }

        private void TransferTokenFromSenderVirtualAddress(string symbol, long amount)
        {
            Context.SendVirtualInline(HashHelper.ComputeFrom(Context.Sender), State.TokenContract.Value,
                nameof(State.TokenContract.Transfer), new TransferInput
                {
                    To = Context.Sender,
                    Symbol = symbol,
                    Amount = amount
                }.ToByteString());
            var currentAmount = GetSenderVirtualAddressBalance(symbol);
            State.ObserverMortgagedTokensMap[Context.Sender] = currentAmount.Sub(amount);
        }

        private long GetSenderVirtualAddressBalance(string symbol)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(Context.Sender));
            return State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = virtualAddress,
                Symbol = symbol
            }).Balance;
        }

        public override Empty AdjustReportFee(Int64Value input)
        {
            Assert(Context.Sender == State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty()),
                "No permission.");
            State.ReportFee.Value = input.Value;
            return new Empty();
        }
    }
}