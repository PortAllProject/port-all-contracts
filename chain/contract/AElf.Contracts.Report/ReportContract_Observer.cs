using AElf.Contracts.MultiToken;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override Empty ApplyObserver(ApplyObserverInput input)
        {
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(Context.Sender)),
                Symbol = State.ObserverMortgageTokenSymbol.Value,
                Amount = State.ApplyObserverFee.Value
            });
            return new Empty();
        }

        public override Empty QuitObserver(QuitObserverInput input)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(Context.Sender));
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = virtualAddress,
                To = Context.Sender,
                Symbol = State.ObserverMortgageTokenSymbol.Value,
                Amount = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = virtualAddress,
                    Symbol = State.ObserverMortgageTokenSymbol.Value
                }).Balance
            });
            return new Empty();
        }

        public override Empty MortgageTokens(Int64Value input)
        {
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(Context.Sender)),
                Symbol = State.ObserverMortgageTokenSymbol.Value,
                Amount = input.Value
            });
            return new Empty();
        }

        public override Empty WithdrawMortgagedTokens(Int64Value input)
        {
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(Context.Sender)),
                To = Context.Sender,
                Symbol = State.ObserverMortgageTokenSymbol.Value,
                Amount = input.Value
            });
            return new Empty();
        }

        public override Empty ProposeAdjustApplyObserverFee(Int64Value input)
        {
            return new Empty();
        }
    }
}