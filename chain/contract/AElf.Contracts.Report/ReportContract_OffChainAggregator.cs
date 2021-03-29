using AElf.Contracts.Association;
using AElf.Standards.ACS3;
using AElf.Types;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override OffChainAggregatorContractInfo AddOffChainAggregator(AddOffChainAggregatorInput input)
        {
            AssertObserversRegistered(input.ObserverList);
            var organizationAddress = CreateObserverAssociation(input.ObserverList);
            var offChainAggregatorContractInfo = new OffChainAggregatorContractInfo
            {
                EthereumContractAddress = input.EthereumContractAddress,
                UrlToQuery = input.UrlToQuery,
                AttributeToFetch = input.AttributeToFetch,
                ConfigDigest = input.ConfigDigest,
                ObserverAssociationAddress = organizationAddress,
                AggregateThreshold = input.AggregateThreshold,
                AggregatorContractAddress = input.AggregatorContractAddress
            };
            State.OffChainAggregatorContractInfoMap[organizationAddress] = offChainAggregatorContractInfo;
            State.CurrentRoundIdMap[organizationAddress] = 1;
            State.CurrentEpochMap[organizationAddress] = 1;
            return offChainAggregatorContractInfo;
        }

        private void AssertObserversRegistered(ObserverList observerList)
        {
            foreach (var address in observerList.Value)
            {
                Assert(State.ObserverMortgagedTokensMap[address] >= State.ApplyObserverFee.Value,
                    $"{address} in observer list is not an observer candidate.");
            }
        }

        private Address CreateObserverAssociation(ObserverList observerList)
        {
            var createOrganizationInput = new CreateOrganizationInput
            {
                CreationToken = HashHelper.ComputeFrom(Context.Self),
                OrganizationMemberList = new OrganizationMemberList {OrganizationMembers = {observerList.Value}},
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 5,
                    MinimalVoteThreshold = 5
                },
                ProposerWhiteList = new ProposerWhiteList {Proposers = {Context.Self}}
            };
            State.AssociationContract.CreateOrganization.Send(createOrganizationInput);
            return State.AssociationContract.CalculateOrganizationAddress.Call(createOrganizationInput);
        }
    }
}