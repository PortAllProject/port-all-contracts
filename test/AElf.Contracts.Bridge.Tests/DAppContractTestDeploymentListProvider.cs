using System.Collections.Generic;
using AElf.Boilerplate.TestBase;
using AElf.ContractTestBase;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Contracts.Bridge.Tests
{
    public class MainChainDAppContractTestDeploymentListProvider : MainChainContractDeploymentListProvider,
        IContractDeploymentListProvider
    {
        public new List<Hash> GetDeployContractNameList()
        {
            var list = base.GetDeployContractNameList();
            list.Add(BridgeSmartContractAddressNameProvider.Name);
            list.Add(OracleSmartContractAddressNameProvider.Name);
            list.Add(RegimentSmartContractAddressNameProvider.Name);
            list.Add(MerkleTreeRecorderSmartContractAddressNameProvider.Name);
            list.Add(MerkleTreeGeneratorSmartContractAddressNameProvider.Name);
            list.Add(StringAggregatorSmartContractAddressNameProvider.Name);
            return list;
        }
    }
}