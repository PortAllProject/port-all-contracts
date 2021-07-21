using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenHolder;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS9;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Lottery
{
    public partial class LotteryContract
    {
        public override Empty TakeContractProfits(TakeContractProfitsInput input)
        {
            Assert(Context.Sender == State.Admin.Value, "No permission.");

            // For Token Holder Profit Scheme. (To distribute.)
            State.TokenHolderContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeManager = Context.Self,
                AmountsMap = {{State.TokenSymbol.Value, 0}}
            });
            return new Empty();
        }

        public override ProfitConfig GetProfitConfig(Empty input)
        {
            return new ProfitConfig
            {
                StakingTokenSymbol = State.TokenSymbol.Value,
                DonationPartsPerHundred = 0,
                ProfitsTokenSymbolList = {State.TokenSymbol.Value}
            };
        }

        public override ProfitsMap GetProfitsAmount(Empty input)
        {
            var profitsMap = new ProfitsMap();
            foreach (var symbol in GetProfitConfig(new Empty()).ProfitsTokenSymbolList)
            {
                var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = Context.Self,
                    Symbol = symbol
                }).Balance;
                profitsMap.Value[symbol] = balance;
            }

            return profitsMap;
        }

        private void InitializeTokenHolderProfitScheme()
        {
            State.TokenHolderContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName);
            State.TokenHolderContract.CreateScheme.Send(new CreateTokenHolderProfitSchemeInput
            {
                Symbol = State.TokenSymbol.Value
            });
        }


        private void ContributeProfits(long contributeAmount)
        {
            State.TokenContract.Approve.Send(new ApproveInput
            {
                Spender = State.TokenHolderContract.Value,
                Symbol = State.TokenSymbol.Value,
                Amount = contributeAmount
            });

            State.TokenHolderContract.ContributeProfits.Send(new ContributeProfitsInput
            {
                SchemeManager = Context.Self,
                Symbol = State.TokenSymbol.Value,
                Amount = contributeAmount
            });
        }
    }
}