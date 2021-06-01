using System;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.Bridge
{
    public partial class BridgeContractState : ContractState
    {
        public MappedState<string, BridgeTokenInfo> BridgeTokenInfoMap { get; set; }
    }
}