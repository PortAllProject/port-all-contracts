using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenHolder;

namespace AElf.Contracts.Lottery
{
    public partial class LotteryContractState
    {
        //internal AEDPoSContractContainer.AEDPoSContractReferenceState AEDPoSContract { get; set; }
        //internal TokenContractImplContainer.TokenContractImplReferenceState TokenContract { get; set; }
        internal TokenHolderContractContainer.TokenHolderContractReferenceState TokenHolderContract { get; set; }
    }
}