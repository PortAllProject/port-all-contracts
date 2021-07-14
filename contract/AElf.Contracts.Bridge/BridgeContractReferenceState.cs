using AElf.Contracts.MerkleTreeGeneratorContract;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.Contracts.Parliament;
using AElf.Contracts.Regiment;
using MTRecorder;

namespace AElf.Contracts.Bridge
{
    public partial class BridgeContractState
    {
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }
        internal OracleContractContainer.OracleContractReferenceState OracleContract { get; set; }
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
        internal RegimentContractContainer.RegimentContractReferenceState RegimentContract { get; set; }

        internal MerkleTreeRecorderContractContainer.MerkleTreeRecorderContractReferenceState
            MerkleTreeRecorderContract { get; set; }

        internal MerkleTreeGeneratorContractContainer.MerkleTreeGeneratorContractReferenceState
            MerkleTreeGeneratorContract { get; set; }
    }
}