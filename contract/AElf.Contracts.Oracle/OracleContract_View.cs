using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Standards.ACS13;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContract
    {
        public override Address GetController(Empty input)
        {
            return State.Controller.Value;
        }

        public override QueryRecord GetQueryRecord(Hash input)
        {
            return State.QueryRecords[input];
        }

        public override CommitmentMap GetCommitmentMap(Hash input)
        {
            var dict = new Dictionary<string, Hash>();
            foreach (var address in GetActualDesignatedNodeList(input).Value)
            {
                var commitment = State.CommitmentMap[input][address];
                if (commitment != null)
                {
                    dict.Add(address.Value.ToHex(), commitment);
                }
            }

            return new CommitmentMap {Value = {dict}};
        }

        public override StringValue GetOracleTokenSymbol(Empty input)
        {
            return new StringValue {Value = TokenSymbol};
        }

        public override Int64Value GetLockedTokensAmount(Address input)
        {
            return new Int64Value
            {
                Value = State.OracleNodesLockedTokenAmountMap[input]
            };
        }

        public override AddressList GetHelpfulNodeList(Hash input)
        {
            return State.HelpfulNodeListMap[input];
        }

        public override QueryTask GetQueryTask(Hash input)
        {
            return State.QueryTaskMap[input];
        }

        public override AddressList GetRegimentMemberList(Address input)
        {
            if (input == State.ParliamentContract.Value)
            {
                return new AddressList
                {
                    Value =
                    {
                        State.ConsensusContract.GetCurrentMinerList.Call(new Empty()).Pubkeys
                            .Select(p => Address.FromPublicKey(p.ToByteArray()))
                    }
                };
            }

            return new AddressList
            {
                Value =
                {
                    State.RegimentContract.GetRegimentMemberList.Call(input).Value
                }
            };
        }

        private Address GetRegimentLockTokenVirtualAddress(Address regimentAddress)
        {
            return Context.ConvertVirtualAddressToContractAddress(HashHelper.ComputeFrom(regimentAddress));
        }
    }
}