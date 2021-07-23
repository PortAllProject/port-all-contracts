using System.Collections.Generic;
using AElf.Boilerplate.TestBase;
using AElf.ContractTestBase;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Contracts.Lottery.Tests
{
    public class SideChainDAppContractTestDeploymentListProvider : SideChainContractDeploymentListProvider,
        IContractDeploymentListProvider
    {
        public List<Hash> GetDeployContractNameList()
        {
            var list = base.GetDeployContractNameList();
            list.Add(LotterySmartContractAddressNameProvider.Name);
            return list;
        }
    }

    public class MainChainDAppContractTestDeploymentListProvider : MainChainContractDeploymentListProvider,
        IContractDeploymentListProvider
    {
        public List<Hash> GetDeployContractNameList()
        {
            var list = base.GetDeployContractNameList();
            list.Add(LotterySmartContractAddressNameProvider.Name);
            return list;
        }
    }
}