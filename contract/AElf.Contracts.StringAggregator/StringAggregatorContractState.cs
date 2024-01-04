using System;
using AElf.Sdk.CSharp.State;

namespace AElf.Contracts.StringAggregator
{
    public class StringAggregatorContractState : ContractState
    {
        public SingletonState<bool> IsEnabled { get; set; }
    }
}