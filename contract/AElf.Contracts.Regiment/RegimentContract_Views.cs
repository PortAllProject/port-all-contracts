using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Regiment
{
    public partial class RegimentContract
    {
        public override Address GetController(Empty input)
        {
            return State.Controller.Value;
        }

        public override RegimentContractConfig GetConfig(Empty input)
        {
            return new RegimentContractConfig
            {
                MaximumAdminsCount = State.MaximumAdminsCount.Value,
                MemberJoinLimit = State.MemberJoinLimit.Value,
                RegimentLimit = State.RegimentLimit.Value
            };
        }

        public override RegimentInfo GetRegimentInfo(Address input)
        {
            return State.RegimentInfoMap[input];
        }

        public override RegimentMemberList GetRegimentMemberList(Address input)
        {
            return State.RegimentMemberListMap[input];
        }

        public override BoolValue IsRegimentMember(IsRegimentMemberInput input)
        {
            var regimentMemberList = State.RegimentMemberListMap[input.RegimentAddress];

            return new BoolValue
            {
                Value = regimentMemberList?.Value != null && regimentMemberList.Value.Contains(input.Address)
            };
        }
    }
}