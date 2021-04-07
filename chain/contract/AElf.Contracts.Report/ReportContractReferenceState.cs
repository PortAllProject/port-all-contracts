using AElf.Contracts.Association;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.Contracts.Parliament;
using AElf.Standards.ACS13;

namespace AElf.Contracts.Report
{
    public partial class ReportContractState
    {
        internal ParliamentContractImplContainer.ParliamentContractImplReferenceState ParliamentContract { get; set; }

        internal OracleContractContainer.OracleContractReferenceState OracleContract { get; set; }

        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }

        internal AssociationContractImplContainer.AssociationContractImplReferenceState AssociationContract
        {
            get;
            set;
        }

        internal OracleAggregatorContractContainer.OracleAggregatorContractReferenceState AggregatorContract
        {
            get;
            set;
        }
        internal AEDPoSContractContainer.AEDPoSContractReferenceState ConsensusContract { get; set; }
    }
}