using System.Collections.Generic;
using AElf.ContractTestBase;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Boilerplate.TestBase
{
    public class SideChainDAppContractTestDeploymentListProvider : SideChainContractDeploymentListProvider, IContractDeploymentListProvider
    {
        public List<Hash> GetDeployContractNameList()
        {
            var list = base.GetDeployContractNameList();
            list.Add(DAppSmartContractAddressNameProvider.Name);
            return list;
        }
    }
    
    public class MainChainDAppContractTestDeploymentListProvider : MainChainContractDeploymentListProvider, IContractDeploymentListProvider
    {
        public List<Hash> GetDeployContractNameList()
        {
            var list = base.GetDeployContractNameList();
            list.Add(DAppSmartContractAddressNameProvider.Name);
            list.Add(OracleUserSmartContractAddressNameProvider.Name);
            list.Add(NumericAggregatorSmartContractAddressNameProvider.Name);
            list.Add(StringAggregatorSmartContractAddressNameProvider.Name);
            list.Add(ReportSmartContractAddressNameProvider.Name);
            list.Add(RegimentSmartContractAddressNameProvider.Name);
            list.Add(BridgeSmartContractAddressNameProvider.Name);
            return list;
        }
    }
}