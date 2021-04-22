using System.Linq;
using AElf.Contracts.Association;
using AElf.Standards.ACS3;
using AElf.Types;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override OffChainAggregationInfo RegisterOffChainAggregation(
            RegisterOffChainAggregationInput input)
        {
            Assert(input.OffChainQueryInfoList.Value.Count >= 1, "At least 1 off-chain info.");
            if (input.OffChainQueryInfoList.Value.Count > 1)
            {
                Assert(input.AggregatorContractAddress != null,
                    "Merkle tree style aggregator must set aggregator contract address.");
            }

            AssertObserversRegistered(input.ObserverList);
            Address organizationAddress;
            if (input.ObserverList.Value.Count == 1 &&
                input.ObserverList.Value.Single() == State.ParliamentContract.Value)
            {
                organizationAddress = State.ParliamentContract.Value;
            }
            else
            {
                organizationAddress = CreateObserverAssociation(input.ObserverList);
            }

            var offChainAggregationInfo = new OffChainAggregationInfo
            {
                EthereumContractAddress = input.EthereumContractAddress,
                OffChainQueryInfoList = input.OffChainQueryInfoList,
                ConfigDigest = input.ConfigDigest,
                ObserverAssociationAddress = organizationAddress,
                AggregateThreshold = input.AggregateThreshold,
                AggregatorContractAddress = input.AggregatorContractAddress
            };
            for (var i = 0; i < input.OffChainQueryInfoList.Value.Count; i++)
            {
                offChainAggregationInfo.RoundIds.Add(0);
            }

            State.OffChainAggregationInfoMap[input.EthereumContractAddress] = offChainAggregationInfo;
            State.CurrentRoundIdMap[input.EthereumContractAddress] = 1;
            return offChainAggregationInfo;
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