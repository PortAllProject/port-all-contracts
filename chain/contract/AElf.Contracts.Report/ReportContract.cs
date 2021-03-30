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
            var offChainAggregatorContract = State.OffChainAggregatorContractInfoMap[input.ObserverAssociationAddress];
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

            Assert(offChainAggregatorContract.OffChainInfo.Count > input.NodeIndex, "Invalid node index.");
            var queryInput = new QueryInput
            {
                Payment = input.Payment,
                AggregateThreshold = Math.Max(offChainAggregatorContract.AggregateThreshold, input.AggregateThreshold),
                // DO NOT FILL THIS FILED.
                // AggregatorContractAddress = null,
                UrlToQuery = offChainAggregatorContract.OffChainInfo[input.NodeIndex].UrlToQuery,
                AttributeToFetch = offChainAggregatorContract.OffChainInfo[input.NodeIndex].AttributeToFetch,
                DesignatedNodeList = new AddressList
                {
                    Value =
                    {
                        // Same to offChainAggregatorContract.ObserverList.Value.First()
                        input.ObserverAssociationAddress
                    }
                },
                CallbackInfo = new CallbackInfo
                {
                    ContractAddress = Context.Self,
                    MethodName = nameof(ProposeReport)
                }
            };
            State.OracleContract.Query.Send(queryInput);

            var queryId = Context.GenerateId(State.OracleContract.Value, HashHelper.ComputeFrom(queryInput));
            State.ReportQueryRecordMap[queryId] = new ReportQueryRecord
            {
                OriginQueryManager = input.QueryManager == null ? Context.Sender : input.QueryManager,
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

            Assert(reportQueryRecord.OriginQueryManager == Context.Sender, "No permission.");
            // Return report fee.
            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = reportQueryRecord.OriginQueryManager,
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

            var currentRoundId = State.CurrentRoundIdMap[nodeDataList.ObserverAssociationAddress];

            var offChainAggregatorContractInfo =
                State.OffChainAggregatorContractInfoMap[nodeDataList.ObserverAssociationAddress];

            Report report;
            if (offChainAggregatorContractInfo.OffChainInfo.Count == 1)
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
                    AggregatedData = GetAggregatedData(offChainAggregatorContractInfo, nodeDataList).Value
                };
                State.ReportMap[nodeDataList.ObserverAssociationAddress][currentRoundId] = report;
                State.CurrentRoundIdMap[nodeDataList.ObserverAssociationAddress] = currentRoundId.Add(1);
                Context.Fire(new ReportProposed
                {
                    ObserverAssociationAddress = nodeDataList.ObserverAssociationAddress,
                    Report = report
                });
            }
            else
            {
                var offChainInfo = new OffChainInfo
                {
                    UrlToQuery = nodeDataList.UrlToQuery,
                    AttributeToFetch = nodeDataList.AttributeToFetch
                };
                var nodeIndex = offChainAggregatorContractInfo.OffChainInfo.IndexOf(offChainInfo);
                var nodeRoundId = offChainAggregatorContractInfo.RoundIds[nodeIndex];
                Assert(nodeRoundId.Add(1) == currentRoundId,
                    $"Data of {offChainInfo} already revealed.{nodeIndex}\n{offChainAggregatorContractInfo}");
                offChainAggregatorContractInfo.RoundIds[nodeIndex] = nodeRoundId.Add(1);
                var aggregatedData = GetAggregatedData(offChainAggregatorContractInfo, nodeDataList);
                report = State.ReportMap[nodeDataList.ObserverAssociationAddress][currentRoundId] ?? new Report
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
                if (offChainAggregatorContractInfo.RoundIds.All(i => i >= currentRoundId))
                {
                    // Time to generate merkle tree.
                    var merkleTree = BinaryMerkleTree.FromLeafNodes(report.Observations.Value
                        .OrderBy(o => int.Parse(o.Key))
                        .Select(o => HashHelper.ComputeFrom(o.Data.ToByteArray())));
                    State.BinaryMerkleTreeMap[nodeDataList.ObserverAssociationAddress][currentRoundId] = merkleTree;
                    report.AggregatedData = merkleTree.Root.Value;
                    Context.Fire(new ReportProposed
                    {
                        ObserverAssociationAddress = nodeDataList.ObserverAssociationAddress,
                        Report = report
                    });
                    State.CurrentRoundIdMap[nodeDataList.ObserverAssociationAddress] = currentRoundId.Add(1);
                }

                State.ReportMap[nodeDataList.ObserverAssociationAddress][currentRoundId] = report;
            }

            return report;
        }

        private BytesValue GetAggregatedData(OffChainAggregatorContractInfo offChainAggregatorContractInfo,
            NodeDataList nodeDataList)
        {
            var aggregatorContractAddress = offChainAggregatorContractInfo.AggregatorContractAddress;
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
            var offChainAggregatorContract = State.OffChainAggregatorContractInfoMap[input.ObserverAssociationAddress];
            if (offChainAggregatorContract == null)
            {
                throw new AssertionException("Observer Association not exists.");
            }

            var organization =
                State.AssociationContract.GetOrganization.Call(offChainAggregatorContract.ObserverAssociationAddress);
            Assert(organization.OrganizationMemberList.OrganizationMembers.Contains(Context.Sender),
                "Sender isn't a member of certain Observer Association.");
            State.ObserverSignatureMap[input.ObserverAssociationAddress][input.RoundId][Context.Sender] =
                input.Signature;
            Context.Fire((new ReportConfirmed {RoundId = input.RoundId, Signature = input.Signature}));
            return new Empty();
        }
    }
}