using AElf.Contracts.Association;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Regiment
{
    public partial class RegimentContract : RegimentContractContainer.RegimentContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.IsInitialized.Value, "Already initialized.");
            State.IsInitialized.Value = true;

            State.Controller.Value = input.Controller ?? Context.Sender;
            State.MemberJoinLimit.Value = input.MemberJoinLimit <= 0 ? DefaultMemberJoinLimit : input.MemberJoinLimit;
            State.RegimentLimit.Value = input.RegimentLimit <= 0 ? DefaultRegimentLimit : input.RegimentLimit;
            State.MaximumAdminsCount.Value =
                input.MaximumAdminsCount <= 0 ? DefaultMaximumAdminsCount : input.MaximumAdminsCount;
            Assert(State.MemberJoinLimit.Value <= State.RegimentLimit.Value, "Incorrect MemberJoinLimit.");

            State.AssociationContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.AssociationContractSystemName);

            return new Empty();
        }

        public override Empty CreateRegiment(CreateRegimentInput input)
        {
            AssertSenderIsController();

            var memberList = input.InitialMemberList;
            if (!memberList.Contains(input.Manager))
            {
                memberList.Add(input.Manager);
            }

            Assert(memberList.Count <= State.RegimentLimit.Value, "Too many initial members.");

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
            Assert(State.RegimentInfoMap[regimentAssociationAddress] == null, "Regiment already exists.");

            var regimentInfo = new RegimentInfo
            {
                Manager = input.Manager,
                CreateTime = Context.CurrentBlockTime,
                IsApproveToJoin = input.IsApproveToJoin
            };
            State.RegimentInfoMap[regimentAssociationAddress] = regimentInfo;

            State.RegimentMemberListMap[regimentAssociationAddress] = new RegimentMemberList {Value = {memberList}};

            Context.Fire(new RegimentCreated
            {
                CreateTime = regimentInfo.CreateTime,
                Manager = regimentInfo.Manager,
                InitialMemberList = new RegimentMemberList {Value = {memberList}},
                RegimentAddress = regimentAssociationAddress
            });

            return new Empty();
        }

        public override Empty JoinRegiment(JoinRegimentInput input)
        {
            AssertSenderIsController();

            var regimentInfo = State.RegimentInfoMap[input.RegimentAddress];
            var regimentMemberList = State.RegimentMemberListMap[input.RegimentAddress];
            Assert(regimentMemberList.Value.Count <= State.RegimentLimit.Value,
                $"Regiment member reached the limit {State.RegimentLimit.Value}.");
            if (regimentInfo.IsApproveToJoin || regimentMemberList.Value.Count >= State.MemberJoinLimit.Value)
            {
                Context.Fire(new NewMemberApplied
                {
                    RegimentAddress = input.RegimentAddress,
                    ApplyMemberAddress = input.NewMemberAddress
                });
            }
            else
            {
                AddMember(input.RegimentAddress, input.NewMemberAddress, null, regimentMemberList);
            }

            return new Empty();
        }

        public override Empty LeaveRegiment(LeaveRegimentInput input)
        {
            AssertSenderIsController();

            var regimentMemberList = State.RegimentMemberListMap[input.RegimentAddress];
            // Just check again.
            Assert(input.LeaveMemberAddress == input.OriginSenderAddress, "No permission.");
            Assert(regimentMemberList.Value.Contains(input.LeaveMemberAddress),
                $"{input.LeaveMemberAddress} is not a member of this regiment.");
            DeleteMember(input.RegimentAddress, input.LeaveMemberAddress, null, regimentMemberList);
            return new Empty();
        }

        public override Empty AddRegimentMember(AddRegimentMemberInput input)
        {
            AssertSenderIsController();

            var regimentInfo = State.RegimentInfoMap[input.RegimentAddress];
            var regimentMemberList = State.RegimentMemberListMap[input.RegimentAddress];
            Assert(regimentMemberList.Value.Count <= State.RegimentLimit.Value,
                $"Regiment member reached the limit {State.RegimentLimit.Value}.");
            Assert(
                regimentInfo.Admins.Contains(input.OriginSenderAddress) ||
                regimentInfo.Manager == input.OriginSenderAddress,
                "Origin sender is not manager or admin of this regiment.");
            AddMember(input.RegimentAddress, input.NewMemberAddress, input.OriginSenderAddress, regimentMemberList);

            return new Empty();
        }

        public override Empty DeleteRegimentMember(DeleteRegimentMemberInput input)
        {
            AssertSenderIsController();

            var regimentInfo = State.RegimentInfoMap[input.RegimentAddress];
            var regimentMemberList = State.RegimentMemberListMap[input.RegimentAddress];

            Assert(
                regimentInfo.Admins.Contains(input.OriginSenderAddress) ||
                regimentInfo.Manager == input.OriginSenderAddress,
                "Origin sender is not manager or admin of this regiment.");
            DeleteMember(input.RegimentAddress, input.DeleteMemberAddress, input.OriginSenderAddress,
                regimentMemberList);

            return new Empty();
        }

        public override Empty ChangeController(Address input)
        {
            AssertSenderIsController();

            State.Controller.Value = input;
            return new Empty();
        }

        public override Empty ResetConfig(RegimentContractConfig input)
        {
            AssertSenderIsController();

            State.MemberJoinLimit.Value =
                input.MemberJoinLimit <= 0 ? State.MemberJoinLimit.Value : input.MemberJoinLimit;
            State.RegimentLimit.Value = input.RegimentLimit <= 0 ? State.RegimentLimit.Value : input.RegimentLimit;
            State.MaximumAdminsCount.Value =
                input.MaximumAdminsCount <= 0 ? State.MaximumAdminsCount.Value : input.MaximumAdminsCount;
            Assert(State.MemberJoinLimit.Value <= State.RegimentLimit.Value, "Incorrect MemberJoinLimit.");

            return new Empty();
        }

        public override Empty TransferRegimentOwnership(TransferRegimentOwnershipInput input)
        {
            AssertSenderIsController();

            var regimentInfo = State.RegimentInfoMap[input.RegimentAddress];
            Assert(regimentInfo.Manager == input.OriginSenderAddress, "No permission.");

            regimentInfo.Manager = input.NewManagerAddress;
            State.RegimentInfoMap[input.RegimentAddress] = regimentInfo;
            return new Empty();
        }

        public override Empty AddAdmins(AddAdminsInput input)
        {
            AssertSenderIsController();

            var regimentInfo = State.RegimentInfoMap[input.RegimentAddress];
            Assert(regimentInfo.Manager == input.OriginSenderAddress, "No permission.");
            foreach (var admin in input.NewAdmins)
            {
                Assert(!regimentInfo.Admins.Contains(admin), $"{admin} is already an admin.");
                regimentInfo.Admins.Add(admin);
            }

            Assert(input.NewAdmins.Count <= State.MaximumAdminsCount.Value,
                $"Admins count cannot greater than {State.MaximumAdminsCount.Value}");
            State.RegimentInfoMap[input.RegimentAddress] = regimentInfo;

            return new Empty();
        }

        public override Empty DeleteAdmins(DeleteAdminsInput input)
        {
            AssertSenderIsController();

            var regimentInfo = State.RegimentInfoMap[input.RegimentAddress];
            Assert(regimentInfo.Manager == input.OriginSenderAddress, "No permission.");
            foreach (var admin in input.DeleteAdmins)
            {
                Assert(regimentInfo.Admins.Contains(admin), $"{admin} is not an admin.");
                regimentInfo.Admins.Remove(admin);
            }

            State.RegimentInfoMap[input.RegimentAddress] = regimentInfo;
            return new Empty();
        }

        private void AssertSenderIsController()
        {
            Assert(Context.Sender == State.Controller.Value, "Sender is not the Controller.");
        }

        private void AddMember(Address regimentAddress, Address newMemberAddress, Address operatorAddress,
            RegimentMemberList currentMemberList)
        {
            Assert(!currentMemberList.Value.Contains(newMemberAddress),
                $"Member {newMemberAddress} already exist in regiment {regimentAddress}.");
            currentMemberList.Value.Add(newMemberAddress);
            State.RegimentMemberListMap[regimentAddress] = currentMemberList;
            Context.Fire(new NewMemberAdded
            {
                RegimentAddress = regimentAddress,
                NewMemberAddress = newMemberAddress,
                OperatorAddress = operatorAddress ?? new Address(),
            });
        }

        private void DeleteMember(Address regimentAddress, Address deleteMemberAddress, Address operatorAddress,
            RegimentMemberList currentMemberList)
        {
            Assert(currentMemberList.Value.Contains(deleteMemberAddress),
                $"Member {deleteMemberAddress} not in regiment {regimentAddress}");
            currentMemberList.Value.Remove(deleteMemberAddress);
            State.RegimentMemberListMap[regimentAddress] = currentMemberList;
            Context.Fire(new RegimentMemberLeft
            {
                RegimentAddress = regimentAddress,
                LeftMemberAddress = deleteMemberAddress,
                OperatorAddress = operatorAddress ?? new Address()
            });
        }
    }
}