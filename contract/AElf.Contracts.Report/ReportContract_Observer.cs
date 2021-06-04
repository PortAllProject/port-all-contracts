using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override Empty ApplyObserver(ApplyObserverInput input)
        {
            var regimentCount = input.RegimentAddressList.Count;
            var totalApplyFee = State.ApplyObserverFee.Value.Mul(regimentCount);
            TransferTokenToSenderVirtualAddress(State.ObserverMortgageTokenSymbol.Value, totalApplyFee);
            foreach (var regimentAddress in input.RegimentAddressList)
            {
                Assert(IsRegimentMember(Context.Sender, regimentAddress),
                    $"Sender is not a member of regiment {regimentAddress}");
                var observerList = State.ObserverListMap[regimentAddress] ?? new ObserverList();
                Assert(!observerList.Value.Contains(Context.Sender),
                    $"Sender is already an observer for regiment {regimentAddress}");
                observerList.Value.Add(Context.Sender);
                State.ObserverListMap[regimentAddress] = observerList;
            }

            return new Empty();
        }

        public override Empty QuitObserver(QuitObserverInput input)
        {
            var currentLockingAmount = GetSenderVirtualAddressBalance(State.ObserverMortgageTokenSymbol.Value);
            var shouldReturnAmount = State.ApplyObserverFee.Value.Mul(input.RegimentAddressList.Count);
            if (currentLockingAmount > 0)
            {
                Context.SendVirtualInline(HashHelper.ComputeFrom(Context.Sender), State.TokenContract.Value,
                    nameof(State.TokenContract.Transfer), new TransferInput
                    {
                        To = Context.Sender,
                        Symbol = State.ObserverMortgageTokenSymbol.Value,
                        Amount = currentLockingAmount
                    }.ToByteString());
                State.ObserverMortgagedTokensMap[Context.Sender] = 0;
            }

            foreach (var regimentAssociationAddress in input.RegimentAddressList)
            {
                var observerList = State.ObserverListMap[regimentAssociationAddress] ?? new ObserverList();
                Assert(observerList.Value.Contains(Context.Sender), $"Sender is not an observer for regiment {regimentAssociationAddress}");
                observerList.Value.Remove(Context.Sender);
                State.ObserverListMap[regimentAssociationAddress] = observerList;
            }

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