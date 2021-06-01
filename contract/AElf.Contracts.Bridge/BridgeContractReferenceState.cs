using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.Contracts.Parliament;

namespace AElf.Contracts.Bridge
{
    public partial class BridgeContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal OracleContractContainer.OracleContractReferenceState OracleContract { get; set; }
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
    }
}