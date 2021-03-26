using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.Contracts.Parliament;

namespace AElf.Contracts.Report
{
    public partial class ReportContractState
    {
        internal OracleContractContainer.OracleContractReferenceState OracleContract { get; set; }

        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }

        internal AssociationContractImplContainer.AssociationContractImplReferenceState AssociationContract
        {
            get;
            set;
        }

        internal ParliamentContractImplContainer.ParliamentContractImplReferenceState ParliamentContract { get; set; }
    }
}