using System.Collections.Generic;
using AElf.Boilerplate.TestBase;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Contracts.Bridge
{
    public class StringAggregatorContractInitializationProvider : IContractInitializationProvider
    {
        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }

        public Hash SystemSmartContractName { get; } = StringAggregatorSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.StringAggregator";
    }
}