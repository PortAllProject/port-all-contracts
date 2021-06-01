using AElf.Contracts.Association;
using AElf.Standards.ACS3;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContract
    {
        public override Empty CreateOracleOrganization(CreateOracleOrganizationInput input)
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
            var organizationAddress =
                State.AssociationContract.CalculateOrganizationAddress.Call(createOrganizationInput);
            State.OracleOrganizationInfoMap[organizationAddress] = new OracleOrganizationInfo
            {
                Creator = Context.Sender,
                CreateTime = Context.CurrentBlockTime
            };
            return new Empty();
        }
    }
}