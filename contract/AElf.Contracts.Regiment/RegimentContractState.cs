using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Regiment
{
    public partial class RegimentContractState : ContractState
    {
        public BoolState IsInitialized { get; set; }
        public SingletonState<Address> Controller { get; set; }
        public SingletonState<int> MemberJoinLimit { get; set; }
        public SingletonState<int> RegimentLimit { get; set; }
        public SingletonState<int> MaximumAdminsCount { get; set; }

        public MappedState<Address, RegimentInfo> RegimentInfoMap { get; set; }
        public MappedState<Address, RegimentMemberList> RegimentMemberListMap { get; set; }
    }
}