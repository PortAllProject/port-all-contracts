using System;
using System.Collections.Generic;
using System.IO;
using AElf.Boilerplate.TestBase;
using AElf.ContractTestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace AElf.Contracts.Lottery.Tests
{
    [DependsOn(typeof(MainChainDAppContractTestModule))]
    public class LotteryContractTestModule : MainChainDAppContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();

            context.Services.AddSingleton<IContractInitializationProvider, LotteryContractInitializationProvider>();
            context.Services.AddSingleton<IContractDeploymentListProvider, MainChainDAppContractTestDeploymentListProvider>();
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>();
            var contractCodes = new Dictionary<string, byte[]>(contractCodeProvider.Codes)
            {
                {
                    new LotteryContractInitializationProvider().ContractCodeName,
                    File.ReadAllBytes(typeof(LotteryContract).Assembly.Location)
                }
            };
            contractCodeProvider.Codes = contractCodes;
        }
    }
}