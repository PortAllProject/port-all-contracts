using System;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS13;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract : ReportContractContainer.ReportContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            State.OracleContract.Value = input.OracleContractAddress;
            State.OracleTokenSymbol.Value = State.OracleContract.GetOracleTokenSymbol.Call(new Empty()).Value;
            State.ObserverMortgageTokenSymbol.Value = State.OracleContract.GetOracleTokenSymbol.Call(new Empty()).Value;
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.AssociationContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.AssociationContractSystemName);
            State.ParliamentContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            State.ConsensusContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            State.ReportFee.Value = input.ReportFee == 0 ? DefaultReportFee : input.ReportFee;
            State.ApplyObserverFee.Value =
                input.ApplyObserverFee == 0 ? DefaultApplyObserverFee : input.ApplyObserverFee;
            State.TokenContract.Approve.Send(new ApproveInput
            {
                Spender = State.OracleContract.Value,
                Symbol = State.OracleTokenSymbol.Value,
                Amount = long.MaxValue
            });
            return new Empty();
        }

        public override Hash QueryOracle(QueryOracleInput input)
        {
            // Assert Observer Association is already registered.
            var offChainAggregatorContract = State.OffChainAggregationInfoMap[input.EthereumContractAddress];
            if (offChainAggregatorContract == null)
            {
                throw new AssertionException("Observer Association not exists.");
            }

            // Pay oracle tokens to this contract, amount: report fee + oracle nodes payment.
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = State.OracleTokenSymbol.Value,
                Amount = State.ReportFee.Value.Add(input.Payment)
            });

            Assert(offChainAggregatorContract.OffChainQueryInfoList.Value.Count > input.NodeIndex,
                "Invalid node index.");
            var queryInput = new QueryInput
            {
                Payment = input.Payment,
                AggregateThreshold = Math.Max(offChainAggregatorContract.AggregateThreshold, input.AggregateThreshold),
                // DO NOT FILL THIS FILED.
                // AggregatorContractAddress = null,
                QueryInfo = new QueryInfo
                {
                    UrlToQuery = offChainAggregatorContract.OffChainQueryInfoList.Value[input.NodeIndex].UrlToQuery,
                    AttributesToFetch =
                    {
                        offChainAggregatorContract.OffChainQueryInfoList.Value[input.NodeIndex].AttributesToFetch
                    }
                },
                DesignatedNodeList = new AddressList
                {
                    Value =
                    {
                        offChainAggregatorContract.ObserverAssociationAddress
                    }
                },
                CallbackInfo = new CallbackInfo
                {
                    ContractAddress = Context.Self,
                    MethodName = nameof(ProposeReport)
                },
                Token = input.EthereumContractAddress
            };
            State.OracleContract.Query.Send(queryInput);

            var queryId = Context.GenerateId(State.OracleContract.Value, HashHelper.ComputeFrom(queryInput));
            State.ReportQueryRecordMap[queryId] = new ReportQueryRecord
            {
                OriginQuerySender = Context.Sender,
                // Record current report fee in case it changes before cancelling this query.
                PaidReportFee = State.ReportFee.Value
            };
            return queryId;
        }

        public override Empty CancelQueryOracle(Hash input)
        {
            var reportQueryRecord = State.ReportQueryRecordMap[input];
            if (reportQueryRecord == null)
            {
                throw new AssertionException("Query not exists or not delegated by Report Contract.");
            }

            Assert(reportQueryRecord.OriginQuerySender == Context.Sender, "No permission.");
            // Return report fee.
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = reportQueryRecord.OriginQuerySender,
                Symbol = State.OracleTokenSymbol.Value,
                Amount = reportQueryRecord.PaidReportFee
            });
            State.OracleContract.CancelQuery.Send(input);
            return new Empty();
        }

        public override Report ProposeReport(CallbackInput input)
        {
            Assert(Context.Sender == State.OracleContract.Value,
                "Only Oracle Contract can propose report.");
            var nodeDataList = new NodeDataList();
            nodeDataList.MergeFrom(input.Result);

            var currentRoundId = State.CurrentRoundIdMap[nodeDataList.Token];

            var offChainAggregationInfo =
                State.OffChainAggregationInfoMap[nodeDataList.Token];

            Report report;
            var configDigest = offChainAggregationInfo.ConfigDigest;
            if (offChainAggregationInfo.OffChainQueryInfoList.Value.Count == 1)
            {
                var originObservations = new Observations
                {
                    Value =
                    {
                        nodeDataList.Value.Select(d => new Observation
                        {
                            Key = d.Address.ToByteArray().ToHex(),
                            Data = d.Data
                        })
                    }
                };
                report = new Report
                {
                    QueryId = input.QueryId,
                    RoundId = currentRoundId,
                    Observations = originObservations,
                    AggregatedData = GetAggregatedData(offChainAggregationInfo, nodeDataList).Value
                };
                State.ReportMap[nodeDataList.Token][currentRoundId] = report;
                State.CurrentRoundIdMap[nodeDataList.Token] = currentRoundId.Add(1);
                report.Observers.Add(new ObserverList {Value = {nodeDataList.Value.Select(d => d.Address)}});
                Context.Fire(new ReportProposed
                {
                    ObserverAssociationAddress = nodeDataList.ObserverAssociationAddress,
                    EthereumContractAddress = nodeDataList.Token,
                    RoundId = currentRoundId,
                    RawReport = GenerateEthereumReport(configDigest, nodeDataList.ObserverAssociationAddress, report)
                });
            }
            else
            {
                var offChainQueryInfo = new OffChainQueryInfo
                {
                    UrlToQuery = nodeDataList.QueryInfo.UrlToQuery,
                    AttributesToFetch = {nodeDataList.QueryInfo.AttributesToFetch}
                };
                var nodeIndex = offChainAggregationInfo.OffChainQueryInfoList.Value.IndexOf(offChainQueryInfo);
                var nodeRoundId = offChainAggregationInfo.RoundIds[nodeIndex];
                Assert(nodeRoundId.Add(1) == currentRoundId,
                    $"Data of {offChainQueryInfo} already revealed.{nodeIndex}\n{offChainAggregationInfo}");
                offChainAggregationInfo.RoundIds[nodeIndex] = nodeRoundId.Add(1);
                var aggregatedData = GetAggregatedData(offChainAggregationInfo, nodeDataList);
                report = State.ReportMap[nodeDataList.Token][currentRoundId] ?? new Report
                {
                    QueryId = input.QueryId,
                    RoundId = currentRoundId,
                    Observations = new Observations()
                };
                report.Observations.Value.Add(new Observation
                {
                    Key = nodeIndex.ToString(),
                    Data = aggregatedData.Value
                });
                State.NodeObserverListMap[nodeDataList.Token][currentRoundId][nodeIndex] = new ObserverList
                {
                    Value = {nodeDataList.Value.Select(d => d.Address)}
                };
                if (offChainAggregationInfo.RoundIds.All(i => i >= currentRoundId))
                {
                    // Time to generate merkle tree.
                    var merkleTree = BinaryMerkleTree.FromLeafNodes(report.Observations.Value
                        .OrderBy(o => int.Parse(o.Key))
                        .Select(o => HashHelper.ComputeFrom(o.Data.ToByteArray())));
                    State.BinaryMerkleTreeMap[nodeDataList.Token][currentRoundId] = merkleTree;
                    report.AggregatedData = merkleTree.Root.Value;

                    for (var i = 0; i < offChainAggregationInfo.OffChainQueryInfoList.Value.Count; i++)
                    {
                        report.Observers.Add(State.NodeObserverListMap[nodeDataList.Token][currentRoundId][i]);
                        State.NodeObserverListMap[nodeDataList.Token][currentRoundId].Remove(i);
                    }

                    Context.Fire(new ReportProposed
                    {
                        ObserverAssociationAddress = nodeDataList.ObserverAssociationAddress,
                        EthereumContractAddress = nodeDataList.Token,
                        RoundId = currentRoundId,
                        RawReport = GenerateEthereumReport(configDigest, nodeDataList.ObserverAssociationAddress,
                            report)
                    });
                    State.CurrentRoundIdMap[nodeDataList.Token] = currentRoundId.Add(1);
                }

                State.ReportMap[nodeDataList.Token][currentRoundId] = report;
            }

            return report;
        }

        private BytesValue GetAggregatedData(OffChainAggregationInfo offChainAggregationInfo,
            NodeDataList nodeDataList)
        {
            var aggregatorContractAddress = offChainAggregationInfo.AggregatorContractAddress;
            if (aggregatorContractAddress == null)
            {
                return new BytesValue();
            }

            State.AggregatorContract.Value = aggregatorContractAddress;
            var aggregateInput = new AggregateInput();
            foreach (var nodeData in nodeDataList.Value)
            {
                aggregateInput.Results.Add(nodeData.Data);
                aggregateInput.Frequencies.Add(1);
            }

            // Use an ACS13 Contract to aggregate a data.
            return State.AggregatorContract.Aggregate.Call(aggregateInput);
        }

        public override Empty ConfirmReport(ConfirmReportInput input)
        {
            // Assert Sender is from certain Observer Association.
            var offChainAggregationInfo = State.OffChainAggregationInfoMap[input.EthereumContractAddress];
            if (offChainAggregationInfo == null)
            {
                throw new AssertionException("Observer Association not exists.");
            }

            var organization =
                State.AssociationContract.GetOrganization.Call(offChainAggregationInfo.ObserverAssociationAddress);
            Assert(organization.OrganizationMemberList.OrganizationMembers.Contains(Context.Sender),
                "Sender isn't a member of certain Observer Association.");
            State.ObserverSignatureMap[input.EthereumContractAddress][input.RoundId][Context.Sender] =
                input.Signature;
            Context.Fire((new ReportConfirmed {RoundId = input.RoundId, Signature = input.Signature}));
            return new Empty();
        }

        public override Empty RejectReport(RejectReportInput input)
        {
            var offChainAggregationInfo = State.OffChainAggregationInfoMap[input.EthereumContractAddress];
            if (offChainAggregationInfo == null)
            {
                throw new AssertionException("Observer Association not exists.");
            }

            Assert(offChainAggregationInfo.OffChainQueryInfoList.Value.Count == 1,
                "Merkle tree style aggregation doesn't support reject.");

            var organization =
                State.AssociationContract.GetOrganization.Call(offChainAggregationInfo.ObserverAssociationAddress);
            Assert(organization.OrganizationMemberList.OrganizationMembers.Contains(Context.Sender),
                "Sender isn't a member of certain Observer Association.");
            foreach (var accusingNode in input.AccusingNodes)
            {
                Assert(organization.OrganizationMemberList.OrganizationMembers.Contains(accusingNode),
                    "Accusing node isn't a member of certain Observer Association.");
            }

            var report = State.ReportMap[input.EthereumContractAddress][input.RoundId];
            var senderData = report.Observations.Value.First(o => o.Key == Context.Sender.ToByteArray().ToHex()).Data;
            foreach (var accusingNode in input.AccusingNodes)
            {
                var accusedNodeData = report.Observations.Value.First(o => o.Key == accusingNode.ToByteArray().ToHex())
                    .Data;
                Assert(!senderData.Equals(accusedNodeData), "Invalid accuse.");
            }


            return new Empty();
        }
    }
}