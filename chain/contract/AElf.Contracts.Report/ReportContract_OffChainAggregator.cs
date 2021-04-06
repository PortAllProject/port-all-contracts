using AElf.Contracts.Association;
using AElf.Standards.ACS3;
using AElf.Types;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override OffChainAggregatorContractInfo AddOffChainAggregator(AddOffChainAggregatorInput input)
        {
            Assert(input.OffChainInfo.Count >= 1, "At least 1 off-chain info.");
            if (input.OffChainInfo.Count > 1)
            {
                Assert(input.AggregatorContractAddress != null,
                    "Merkle tree style aggregator must set aggregator contract address.");
            }

            AssertObserversRegistered(input.ObserverList);
            var organizationAddress = CreateObserverAssociation(input.ObserverList);
            var offChainAggregatorContractInfo = new OffChainAggregatorContractInfo
            {
                EthereumContractAddress = input.EthereumContractAddress,
                OffChainInfo = {input.OffChainInfo},
                ConfigDigest = input.ConfigDigest,
                ObserverAssociationAddress = organizationAddress,
                AggregateThreshold = input.AggregateThreshold,
                AggregatorContractAddress = input.AggregatorContractAddress
            };
            for (var i = 0; i < input.OffChainInfo.Count; i++)
            {
                offChainAggregatorContractInfo.RoundIds.Add(0);
            }

            State.OffChainAggregatorContractInfoMap[input.EthereumContractAddress] = offChainAggregatorContractInfo;
            State.CurrentRoundIdMap[input.EthereumContractAddress] = 1;
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