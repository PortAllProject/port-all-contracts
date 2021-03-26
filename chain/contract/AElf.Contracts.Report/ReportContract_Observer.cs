using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override Empty ApplyObserver(ApplyObserverInput input)
        {
            TransferTokenToSenderVirtualAddress(State.ObserverMortgageTokenSymbol.Value, State.ApplyObserverFee.Value);
            return new Empty();
        }

        public override Empty QuitObserver(QuitObserverInput input)
        {
            var currentAmount = GetSenderVirtualAddressBalance(State.ObserverMortgageTokenSymbol.Value);
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(Context.Sender)),
                To = Context.Sender,
                Symbol = State.ObserverMortgageTokenSymbol.Value,
                Amount = currentAmount
            });
            State.ObserverMortgagedTokensMap[Context.Sender] = 0;
            return new Empty();
        }

        public override Empty MortgageTokens(Int64Value input)
        {
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
            return new Empty();
        }

        private void TransferTokenToSenderVirtualAddress(string symbol, long amount)
        {
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(Context.Sender)),
                Symbol = symbol,
                Amount = amount
            });
            var currentAmount = GetSenderVirtualAddressBalance(symbol);
            State.ObserverMortgagedTokensMap[Context.Sender] = currentAmount.Add(amount);
        }

        private void TransferTokenFromSenderVirtualAddress(string symbol, long amount)
        {
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(Context.Sender)),
                To = Context.Sender,
                Symbol = symbol,
                Amount = amount
            });
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
    }
}