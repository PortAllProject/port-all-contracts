using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Boilerplate.TestBase;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Lottery.Tests
{
    public class LotteryContractTestBase : DAppContractTestBase<LotteryContractTestModule>
    {
        protected Address DefaultSender { get; set; }
        internal IList<LotteryContractContainer.LotteryContractStub> UserStubs { get; set; }
        internal List<Account> Users { get; set; }
        internal LotteryContractContainer.LotteryContractStub Admin { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal List<TokenContractContainer.TokenContractStub> UserTokenContractStubs { get; set; }
        internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub { get; set; }

        protected LotteryContractTestBase()
        {
            var keyPair = SampleAccount.Accounts.First().KeyPair;
            DefaultSender = SampleAccount.Accounts.First().Address;
            TokenContractStub = GetTokenContractStub(keyPair);
            ParliamentContractStub = GetParliamentContractStub(keyPair);
            Admin = GetLotteryContractStub(keyPair);
            Users = SampleAccount.Accounts.Skip(1).Take(20).ToList();
            UserStubs = Users.Select(a => GetLotteryContractStub(a.KeyPair))
                .ToList();
            UserTokenContractStubs = Users.Select(a => GetTokenContractStub(a.KeyPair))
                .ToList();
        }

        internal LotteryContractContainer.LotteryContractStub GetLotteryContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<LotteryContractContainer.LotteryContractStub>(DAppContractAddress, senderKeyPair);
        }

        internal TokenContractContainer.TokenContractStub GetTokenContractStub(ECKeyPair senderKeyPair)
        {
            return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, senderKeyPair);
        }

        internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractStub(
            ECKeyPair senderKeyPair)
        {
            return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
                senderKeyPair);
        }

        internal AssociationContractContainer.AssociationContractStub GetAssociationContractStub(
            ECKeyPair senderKeyPair)
        {
            return GetTester<AssociationContractContainer.AssociationContractStub>(AssociationContractAddress,
                senderKeyPair);
        }

        protected IEnumerable<Account> GetNodes(int count)
        {
            return SampleAccount.Accounts.Skip(1).Take(count).ToList();
        }

        protected async Task<Address> GetDefaultParliament()
        {
            return await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        }

        internal async Task ParliamentProposeAndRelease(CreateProposalInput proposal)
        {
            var ret = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
            var proposalId = Hash.Parser.ParseFrom(ret.TransactionResult.ReturnValue);
            await ParliamentContractStub.Approve.SendAsync(proposalId);
            await ParliamentContractStub.Release.SendAsync(proposalId);
        }
    }
}