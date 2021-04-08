using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.Contracts.Parliament;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.CrossChainToken
{
    public class CrossChainTokenState : ContractState
    {
        internal OracleContractContainer.OracleContractReferenceState OracleContract { get; set; }
        internal ParliamentContractContainer.ParliamentContractReferenceState ParliamentContract { get; set; }
        internal TokenContractContainer.TokenContractReferenceState TokenContract { get; set; }

        public MappedState<Address, string, long> Balances { get; set; }

        public MappedState<Address, Address, string, long> Allowances { get; set; }

        public StringState OracleTokenSymbol { get; set; }

        public MappedState<string, bool> CrossChainTransferFinishedMap { get; set; }
    }
}