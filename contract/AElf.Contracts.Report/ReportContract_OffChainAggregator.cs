using System;
using System.Collections.Generic;
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

            Assert(input.OffChainQueryInfoList.Value.Count <= MaximumOffChainQueryInfoCount,
                $"Maximum off chain query info count: {MaximumOffChainQueryInfoCount}");

            var regimentInfo = State.RegimentContract.GetRegimentInfo.Call(input.RegimentAddress);
            Assert(regimentInfo.Manager != null, "Regiment not exists.");

            var offChainAggregationInfo = new OffChainAggregationInfo
            {
                Token = input.Token,
                OffChainQueryInfoList = input.OffChainQueryInfoList,
                ConfigDigest = input.ConfigDigest,
                RegimentAddress = input.RegimentAddress,
                AggregateThreshold = input.AggregateThreshold,
                AggregatorContractAddress = input.AggregatorContractAddress,
                ChainName = input.ChainName,
                Register = Context.Sender,
                AggregateOption = input.AggregateOption
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
                RegimentAddress = offChainAggregationInfo.RegimentAddress,
                AggregateThreshold = offChainAggregationInfo.AggregateThreshold,
                AggregatorContractAddress = offChainAggregationInfo.AggregatorContractAddress,
                ChainName = offChainAggregationInfo.ChainName,
                Register = offChainAggregationInfo.Register,
                AggregateOption = offChainAggregationInfo.AggregateOption
            });

            return offChainAggregationInfo;
        }

        public override OffChainAggregationInfo BindOffChainAggregation(BindOffChainAggregationInput input)
        {
            Address regimentAssociationAddress = null;
            Address aggregatorContractAddress = null;
            var queryInfoList = new OffChainQueryInfoList();
            var aggregatedThreshold = 0;
            var aggregateOptions = new List<int>();
            foreach (var taskId in input.TaskIdList)
            {
                var queryTask = State.OracleContract.GetQueryTask.Call(taskId);
                Assert(
                    regimentAssociationAddress == null ||
                    queryTask.DesignatedNodeList.Value.First() == regimentAssociationAddress,
                    "Designated node is not same.");
                Assert(
                    aggregatorContractAddress == null ||
                    queryTask.AggregatorContractAddress == aggregatorContractAddress,
                    "AggregatorContractAddress is not same.");
                regimentAssociationAddress = queryTask.DesignatedNodeList.Value.First();
                aggregatorContractAddress = queryTask.AggregatorContractAddress;
                queryInfoList.Value.Add(new OffChainQueryInfo
                {
                    Title = queryTask.QueryInfo.Title,
                    Options = {queryTask.QueryInfo.Options}
                });
                aggregatedThreshold = Math.Max(aggregatedThreshold, queryTask.AggregateThreshold);
                aggregateOptions.Add(queryTask.AggregateOption);
                if (aggregateOptions.Count > 0)
                {
                    Assert(aggregateOptions.Contains(queryTask.AggregateOption), "AggregateOption is not same.");
                }
            }

            var registerOffChainAggregationInput = new RegisterOffChainAggregationInput
            {
                Token = input.Token,
                OffChainQueryInfoList = queryInfoList,
                ConfigDigest = input.ConfigDigest,
                RegimentAddress = regimentAssociationAddress,
                AggregateThreshold = aggregatedThreshold,
                AggregatorContractAddress = aggregatorContractAddress,
                ChainName = input.ChainName,
                Register = Context.Sender,
                AggregateOption = aggregateOptions.First()
            };

            return RegisterOffChainAggregation(registerOffChainAggregationInput);
        }

        public override Empty AddOffChainQueryInfo(AddOffChainQueryInfoInput input)
        {
            var offChainAggregationInfo = State.OffChainAggregationInfoMap[input.Token];
            if (offChainAggregationInfo == null)
            {
                throw new AssertionException($"Token {input.Token} not registered.");
            }
            Assert(offChainAggregationInfo.Register == Context.Sender, "No permission.");
            Assert(offChainAggregationInfo.OffChainQueryInfoList.Value.Count > 1,
                "Only merkle style aggregation can manage off chain query info.");
            offChainAggregationInfo.OffChainQueryInfoList.Value.Add(input.OffChainQueryInfo);
            offChainAggregationInfo.RoundIds.Add(State.CurrentRoundIdMap[input.Token].Sub(1));
            Assert(offChainAggregationInfo.OffChainQueryInfoList.Value.Count <= MaximumOffChainQueryInfoCount,
                $"Maximum off chain query info count: {MaximumOffChainQueryInfoCount}");
            State.OffChainAggregationInfoMap[input.Token] = offChainAggregationInfo;
            return new Empty();
        }

        public override Empty RemoveOffChainQueryInfo(RemoveOffChainQueryInfoInput input)
        {
            var offChainAggregationInfo = State.OffChainAggregationInfoMap[input.Token];
            if (offChainAggregationInfo == null)
            {
                throw new AssertionException($"Token {input.Token} not registered.");
            }
            Assert(offChainAggregationInfo.Register == Context.Sender, "No permission.");
            Assert(offChainAggregationInfo.OffChainQueryInfoList.Value.Count > 1,
                "Only merkle style aggregation can manage off chain query info.");
            Assert(offChainAggregationInfo.OffChainQueryInfoList.Value.Count > input.RemoveNodeIndex, "Invalid index.");
            offChainAggregationInfo.OffChainQueryInfoList.Value[input.RemoveNodeIndex] =
                new OffChainQueryInfo
                {
                    Title = "invalid"
                };
            offChainAggregationInfo.RoundIds[input.RemoveNodeIndex] = -1;
            State.OffChainAggregationInfoMap[input.Token] = offChainAggregationInfo;
            return new Empty();
        }

        public override Empty ChangeOffChainQueryInfo(ChangeOffChainQueryInfoInput input)
        {
            var offChainAggregationInfo = State.OffChainAggregationInfoMap[input.Token];
            if (offChainAggregationInfo == null)
            {
                throw new AssertionException($"Token {input.Token} not registered.");
            }
            Assert(offChainAggregationInfo.Register == Context.Sender, "No permission.");
            Assert(offChainAggregationInfo.OffChainQueryInfoList.Value.Count == 1,
                "Only single style aggregation can change off chain query info.");
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
    }
}