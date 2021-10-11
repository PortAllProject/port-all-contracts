using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Oracle;
using AElf.Types;
using Xunit;

namespace AElf.Contracts.Bridge.Tests
{
    public class BridgeContractTests : BridgeContractTestBase
    {
        [Fact]
        public async Task PipelineTest()
        {
            await InitialSwapAsync();

            // 
        }

        private async Task InitialSwapAsync()
        {
            await InitializeOracleContractAsync();
            await InitializeBridgeContractAsync();

            // Create regiment.
            await OracleContractStub.CreateRegiment.SendAsync(new CreateRegimentInput
            {
                Manager = DefaultSenderAddress,
                InitialMemberList = {Transmitters.Select(a => a.Address)}
            });

            var regimentAddress = Address.FromBase58("fsEW3n8zHD1g4rMKouMFMaXfR1d155ag5N1eoXEiLNQH56aKy");

            // Create swap.
            await BridgeContractStub.CreateSwap.SendAsync(new CreateSwapInput
            {
                OriginTokenNumericBigEndian = true,
                OriginTokenSizeInByte = 32,
                RegimentAddress = regimentAddress
            });
        }

        private async Task InitializeOracleContractAsync()
        {
            await OracleContractStub.Initialize.SendAsync(new Oracle.InitializeInput
            {
                RegimentContractAddress = RegimentContractAddress
            });
        }

        private async Task InitializeBridgeContractAsync()
        {
            await BridgeContractStub.Initialize.SendAsync(new InitializeInput
            {
                MerkleTreeGeneratorContractAddress = MerkleTreeGeneratorContractAddress,
                MerkleTreeRecorderContractAddress = MerkleTreeRecorderContractAddress,
                MerkleTreeLeafLimit = 16,
                OracleContractAddress = OracleContractAddress,
                RegimentContractAddress = RegimentContractAddress
            });
        }
    }
}