using System.Collections.Generic;
using AElf.Boilerplate.TestBase;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Contracts.Bridge
{
    public class MerkleTreeRecorderInitializationProvider : IContractInitializationProvider, ISingletonDependency
    {
        public List<ContractInitializationMethodCall> GetInitializeMethodList(byte[] contractCode)
        {
            return new List<ContractInitializationMethodCall>();
        }

        public Hash SystemSmartContractName { get; } = MerkleTreeRecorderSmartContractAddressNameProvider.Name;
        public string ContractCodeName { get; } = "AElf.Contracts.MerkleTreeRecorder";
    }
}