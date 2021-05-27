using System;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override Empty ApplyObserver(Empty input)
        {
            Assert(!IsValidObserver(Context.Sender, out var virtualAddressBalance), "Sender is an observer.");
            var actualApplyFee = Math.Max(0, State.ApplyObserverFee.Value.Sub(virtualAddressBalance));
            TransferTokenToSenderVirtualAddress(State.ObserverMortgageTokenSymbol.Value, actualApplyFee);
            State.ObserverMap[Context.Sender] = true;
            return new Empty();
        }

        public override Empty QuitObserver(Empty input)
        {
            Assert(State.ObserverMap[Context.Sender], "Sender is not an observer.");
            var currentAmount = GetSenderVirtualAddressBalance(State.ObserverMortgageTokenSymbol.Value);
            if (currentAmount > 0)
            {
                Context.SendVirtualInline(HashHelper.ComputeFrom(Context.Sender), State.TokenContract.Value,
                    nameof(State.TokenContract.Transfer), new TransferInput
                    {
                        To = Context.Sender,
                        Symbol = State.ObserverMortgageTokenSymbol.Value,
                        Amount = currentAmount
                    }.ToByteString());
                State.ObserverMortgagedTokensMap[Context.Sender] = 0;
            }

            State.ObserverMap[Context.Sender] = false;

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

        public override Empty AdjustApplyObserverFee(Int64Value input)
        {
            Assert(Context.Sender == State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty()),
                "No permission.");
            State.ApplyObserverFee.Value = input.Value;
            return new Empty();
        }

        private void TransferTokenToSenderVirtualAddress(string symbol, long amount)
        {
            if (amount <= 0) return;
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
            if (amount <= 0) return;
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
            return GetVirtualAddressBalance(symbol, Context.Sender);
        }

        private long GetVirtualAddressBalance(string symbol, Address address)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(address));
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

        public override Int64Value GetMortgagedTokenAmount(Address input)
        {
            var virtualAddress = Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(input));
            return new Int64Value
            {
                Value = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = virtualAddress,
                    Symbol = State.ObserverMortgageTokenSymbol.Value
                }).Balance
            };
        }
    }
}