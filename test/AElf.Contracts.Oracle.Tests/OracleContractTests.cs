using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Standards.ACS3;
using Google.Protobuf;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContractTests : OracleContractTestBase
    {
        private async Task InitializeOracleContractAsync()
        {
            await OracleContractStub.InitializeAndCreateToken.SendAsync(new InitializeInput
            {
                MinimumOracleNodesCount = DefaultMinimumOracleNodesCount,
                DefaultRevealThreshold = DefaultRevealThreshold,
                DefaultAggregateThreshold = DefaultAggregateThreshold,
                DefaultExpirationSeconds = DefaultExpirationSeconds,
                IsChargeFee = true,
                RegimentContractAddress = RegimentContractAddress
            });
        }

        private async Task ChangeTokenIssuerToDefaultSenderAsync()
        {
            await ParliamentProposeAndRelease(new CreateProposalInput
            {
                ToAddress = TokenContractAddress,
                ContractMethodName = nameof(TokenContractContainer.TokenContractStub.ChangeTokenIssuer),
                OrganizationAddress = await GetDefaultParliament(),
                Params = new ChangeTokenIssuerInput
                {
                    NewTokenIssuer = DefaultSender,
                    Symbol = TokenSymbol
                }.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            });
        }
    }
}