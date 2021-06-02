using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Standards.ACS3;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContract
    {
        public override Empty CreateRegiment(CreateRegimentInput input)
        {
            // Need to pay.

            var createOrganizationInput = new CreateOrganizationInput
            {
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers = {Context.Self}
                },
                ProposerWhiteList = new ProposerWhiteList {Proposers = {Context.Self}},
                CreationToken = HashHelper.ComputeFrom(input),
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 1,
                    MaximalRejectionThreshold = 0,
                    MaximalAbstentionThreshold = 0
                }
            };
            State.AssociationContract.CreateOrganization.Send(createOrganizationInput);
            var regimentAssociationAddress =
                State.AssociationContract.CalculateOrganizationAddress.Call(createOrganizationInput);
            State.RegimentInfoMap[regimentAssociationAddress] = new RegimentInfo
            {
                Manager = Context.Sender,
                CreateTime = Context.CurrentBlockTime
            };

            var lockTokenVirtualAddress = GetRegimentLockTokenVirtualAddress(regimentAssociationAddress);
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = lockTokenVirtualAddress,
                Symbol = TokenSymbol
            });
            return new Empty();
        }
    }
}