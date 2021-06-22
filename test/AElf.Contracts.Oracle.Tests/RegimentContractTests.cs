using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Regiment;
using AElf.ContractTestKit;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContractTests
    {
        private readonly Account _controllerAccount = SampleAccount.Accounts[0];

        private RegimentContractContainer.RegimentContractStub ControllerStub =>
            GetRegimentContractStub(_controllerAccount.KeyPair); 

        [Fact]
        public async Task InitialRegimentTest()
        {
            await ControllerStub.Initialize.SendAsync(new Regiment.InitializeInput());
            
            // Check default config.
            var config = await ControllerStub.GetConfig.CallAsync(new Empty());
            config.RegimentLimit.ShouldBe(1024);
            config.MemberJoinLimit.ShouldBe(256);
            config.MaximumAdminsCount.ShouldBe(3);

            var controller = await ControllerStub.GetController.CallAsync(new Empty());
            controller.ShouldBe(_controllerAccount.Address);
        }

        [Fact]
        public async Task CreateRegimentTest()
        {
            var managerAccount = SampleAccount.Accounts[1];
            var managerStub = GetRegimentContractStub(managerAccount.KeyPair);

            await managerStub.CreateRegiment.SendAsync(new Regiment.CreateRegimentInput
            {
                InitialMemberList = {SampleAccount.Accounts.Skip(2).Take(3).Select(a => a.Address)}
            });
            
            
        }
    }
}