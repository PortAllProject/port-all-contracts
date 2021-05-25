using System.Linq;
using AElf.Contracts.Association;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override OffChainAggregationInfo RegisterOffChainAggregation(
            RegisterOffChainAggregationInput input)
        {
            Assert(State.RegisterWhiteListMap[Context.Sender], "Sender not in register white list.");
            Assert(State.OffChainAggregationInfoMap[input.Token] == null,
                $"Off chain aggregation info of {input.Token} already registered.");
            Assert(input.OffChainQueryInfoList.Value.Count >= 1, "At least 1 off-chain info.");
            if (input.OffChainQueryInfoList.Value.Count > 1)
            {
                Assert(input.AggregatorContractAddress != null,
                    "Merkle tree style aggregator must set aggregator contract address.");
            }

            Address organizationAddress;
            if (input.ObserverList.Value.Count == 1)
            {
                // Using an already-exist organization.

                organizationAddress = input.ObserverList.Value.First();
                if (organizationAddress != State.ParliamentContract.Value)
                {
                    var maybeOrganization = State.AssociationContract.GetOrganization.Call(organizationAddress);
                    if (maybeOrganization == null)
                    {
                        throw new AssertionException("Association not exists.");
                    }

                    AssertObserversQualified(new ObserverList
                        {Value = {maybeOrganization.OrganizationMemberList.OrganizationMembers}});
                }
            }
            else
            {
                AssertObserversQualified(input.ObserverList);
                organizationAddress = CreateObserverAssociation(input.ObserverList);
            }

            var offChainAggregationInfo = new OffChainAggregationInfo
            {
                Token = input.Token,
                OffChainQueryInfoList = input.OffChainQueryInfoList,
                ConfigDigest = input.ConfigDigest,
                ObserverAssociationAddress = organizationAddress,
                AggregateThreshold = input.AggregateThreshold,
                AggregatorContractAddress = input.AggregatorContractAddress,
                ChainType = input.ChainType,
                Register = Context.Sender
            };
            for (var i = 0; i < input.OffChainQueryInfoList.Value.Count; i++)
            {
                offChainAggregationInfo.RoundIds.Add(0);
            }

            State.OffChainAggregationInfoMap[input.Token] = offChainAggregationInfo;
            State.CurrentRoundIdMap[input.Token] = 1;

            Context.Fire(new OffChainAggregationRegistered
            {
                Token = offChainAggregationInfo.Token,
                OffChainQueryInfoList = offChainAggregationInfo.OffChainQueryInfoList,
                ConfigDigest = offChainAggregationInfo.ConfigDigest,
                ObserverAssociationAddress = offChainAggregationInfo.ObserverAssociationAddress,
                AggregateThreshold = offChainAggregationInfo.AggregateThreshold,
                AggregatorContractAddress = offChainAggregationInfo.AggregatorContractAddress,
                ChainType = offChainAggregationInfo.ChainType,
                Register = offChainAggregationInfo.Register
            });

            return offChainAggregationInfo;
        }

        public override Empty AddOffChainQueryInfo(AddOffChainQueryInfoInput input)
        {
            var offChainAggregationInfo = State.OffChainAggregationInfoMap[input.Token];
            Assert(offChainAggregationInfo.Register == Context.Sender, "No permission.");
            Assert(offChainAggregationInfo.OffChainQueryInfoList.Value.Count > 1, "Only merkle style aggregation can manage off chain query info.");
            offChainAggregationInfo.OffChainQueryInfoList.Value.Add(input.OffChainQueryInfo);
            offChainAggregationInfo.RoundIds.Add(State.CurrentRoundIdMap[input.Token].Sub(1));
            State.OffChainAggregationInfoMap[input.Token] = offChainAggregationInfo;
            return new Empty();
        }

        public override Empty RemoveOffChainQueryInfo(RemoveOffChainQueryInfoInput input)
        {
            var offChainAggregationInfo = State.OffChainAggregationInfoMap[input.Token];
            Assert(offChainAggregationInfo.Register == Context.Sender, "No permission.");
            Assert(offChainAggregationInfo.OffChainQueryInfoList.Value.Count > 1, "Only merkle style aggregation can manage off chain query info.");
            Assert(offChainAggregationInfo.OffChainQueryInfoList.Value.Count > input.RemoveNodeIndex, "Invalid index.");
            offChainAggregationInfo.OffChainQueryInfoList.Value[input.RemoveNodeIndex] =
                new OffChainQueryInfo
                {
                    UrlToQuery = "invalid"
                };
            offChainAggregationInfo.RoundIds[input.RemoveNodeIndex] = -1;
            State.OffChainAggregationInfoMap[input.Token] = offChainAggregationInfo;
            return new Empty();
        }

        public override Empty ChangeOffChainQueryInfo(ChangeOffChainQueryInfoInput input)
        {
            var offChainAggregationInfo = State.OffChainAggregationInfoMap[input.Token];
            Assert(offChainAggregationInfo.Register == Context.Sender, "No permission.");
            Assert(offChainAggregationInfo.OffChainQueryInfoList.Value.Count == 1, "Only single style aggregation can change off chain query info.");
            offChainAggregationInfo.OffChainQueryInfoList.Value[0] = input.NewOffChainQueryInfo;
            State.OffChainAggregationInfoMap[input.Token] = offChainAggregationInfo;
            return new Empty();
        }

        public override Empty AddRegisterWhiteList(Address input)
        {
            Assert(Context.Sender == State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty()),
                "No permission.");
            Assert(!State.RegisterWhiteListMap[input], $"{input} already in register white list.");
            State.RegisterWhiteListMap[input] = true;
            return new Empty();
        }

        public override Empty RemoveFromRegisterWhiteList(Address input)
        {
            Assert(Context.Sender == State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty()),
                "No permission.");
            Assert(State.RegisterWhiteListMap[input], $"{input} is not in register white list.");
            State.RegisterWhiteListMap[input] = false;
            return new Empty();
        }

        private void AssertObserversQualified(ObserverList observerList)
        {
            foreach (var address in observerList.Value)
            {
                AssertObserverQualified(address);
            }
        }

        private void AssertObserverQualified(Address address)
        {
            Assert(
                State.ObserverMortgagedTokensMap[address] >= State.ApplyObserverFee.Value && State.ObserverMap[address],
                $"{address} is not an observer candidate or mortgaged token not enough.");
        }

        private Address CreateObserverAssociation(ObserverList observerList)
        {
            var createOrganizationInput = new CreateOrganizationInput
            {
                CreationToken = HashHelper.ComputeFrom(Context.Self),
                OrganizationMemberList = new OrganizationMemberList {OrganizationMembers = {observerList.Value}},
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 1
                },
                ProposerWhiteList = new ProposerWhiteList {Proposers = {Context.Self}}
            };
            State.AssociationContract.CreateOrganization.Send(createOrganizationInput);
            return State.AssociationContract.CalculateOrganizationAddress.Call(createOrganizationInput);
        }
    }
}