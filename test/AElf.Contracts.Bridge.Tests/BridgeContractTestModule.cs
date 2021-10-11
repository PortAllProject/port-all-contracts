using System;
using System.Collections.Generic;
using System.IO;
using AElf.Boilerplate.TestBase;
using AElf.Contracts.MerkleTreeRecorder;
using AElf.Contracts.Oracle;
using AElf.Contracts.Regiment;
using AElf.Contracts.StringAggregator;
using AElf.ContractTestBase;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Contracts.Bridge.Tests
{
    [DependsOn(typeof(MainChainDAppContractTestModule))]
    public class BridgeContractTestModule: MainChainDAppContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IBlockTimeProvider, BlockTimeProvider>();
            context.Services.AddSingleton<IContractInitializationProvider, MerkleTreeGeneratorInitializationProvider>();
            context.Services.AddSingleton<IContractInitializationProvider, MerkleTreeRecorderInitializationProvider>();
            context.Services.AddSingleton<IContractDeploymentListProvider, MainChainDAppContractTestDeploymentListProvider>();
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {
            var contractCodeProvider = context.ServiceProvider.GetService<IContractCodeProvider>() ??
                                       new ContractCodeProvider();
            var contractCodes = new Dictionary<string, byte[]>(contractCodeProvider.Codes)
            {
                {
                    new BridgeContractInitializationProvider().ContractCodeName,
                    File.ReadAllBytes(typeof(BridgeContract).Assembly.Location)
                },
                {
                    new MerkleTreeGeneratorInitializationProvider().ContractCodeName,
                    File.ReadAllBytes(typeof(MerkleTreeGeneratorContract.MerkleTreeGeneratorContract).Assembly.Location)
                },
                {
                    new MerkleTreeRecorderInitializationProvider().ContractCodeName,
                    File.ReadAllBytes(typeof(MerkleTreeRecorderContract).Assembly.Location)
                },
                {
                    new StringAggregatorContractInitializationProvider().ContractCodeName,
                    File.ReadAllBytes(typeof(StringAggregatorContract).Assembly.Location)
                },
                {
                    new RegimentContractInitializationProvider().ContractCodeName,
                    File.ReadAllBytes(typeof(RegimentContract).Assembly.Location)
                },
                {
                    new OracleContractInitializationProvider().ContractCodeName,
                    File.ReadAllBytes(typeof(OracleContract).Assembly.Location)
                }
            };
            contractCodeProvider.Codes = contractCodes;
        }
    }
}