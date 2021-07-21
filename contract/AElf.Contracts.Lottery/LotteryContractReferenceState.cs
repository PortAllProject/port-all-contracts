using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenHolder;
using AElf.Standards.ACS6;

namespace AElf.Contracts.Lottery
{
    public partial class LotteryContractState
    {
        internal RandomNumberProviderContractContainer.RandomNumberProviderContractReferenceState
            RandomNumberProviderContract { get; set; }

        internal TokenContractImplContainer.TokenContractImplReferenceState TokenContract { get; set; }
        internal TokenHolderContractContainer.TokenHolderContractReferenceState TokenHolderContract { get; set; }
    }
}