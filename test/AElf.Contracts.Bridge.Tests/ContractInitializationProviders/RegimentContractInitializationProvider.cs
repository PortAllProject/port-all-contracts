using System.Collections.Generic;
using AElf.Boilerplate.TestBase;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.Bridge
{
    public class RegimentContractInitializationProvider : IContractInitializationProvider, ISingletonDependency
    {
        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }

        public Hash SystemSmartContractName { get; } = RegimentSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.Regiment";
    }
}