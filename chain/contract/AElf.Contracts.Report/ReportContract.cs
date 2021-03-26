using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract : ReportContractContainer.ReportContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            State.OracleContract.Value = input.TokenContractAddress;
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
            Assert(input.DesignatedNodes.Count == 1, "Invalid designated nodes.");
            var observerAssociationAddress = input.DesignatedNodes.First();
            // Assert Observer Association is already registered.
            var offChainAggregatorContract = State.OffChainAggregatorContractInfoMap[observerAssociationAddress];
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

            var queryInput = new QueryInput
            {
                Payment = input.Payment,
                AggregateThreshold = offChainAggregatorContract.Threshold,
                AggregatorContractAddress = input.AggregatorContractAddress,
                UrlToQuery = offChainAggregatorContract.UrlToQuery,
                AttributeToFetch = offChainAggregatorContract.AttributeToFetch,
                DesignatedNodeList = new AddressList {Value = {observerAssociationAddress}},
                QueryManager = Context.Self,
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
                OriginQueryManager = Context.Sender,
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
            var observations = new Observations
            {
                Value =
                {
                    nodeDataList.Value.Select(d => new Observation
                    {
                        Address = d.Address,
                        Data = d.Data
                    })
                }
            };
            var currentRoundId = State.CurrentRoundIdMap[nodeDataList.ObserverAssociationAddress];
            var report = new Report
            {
                QueryId = input.QueryId,
                EpochNumber = State.CurrentEpochMap[nodeDataList.ObserverAssociationAddress],
                RoundId = currentRoundId,
                Observations = observations
            };
            State.ReportMap[nodeDataList.ObserverAssociationAddress][currentRoundId] = report;
            State.CurrentRoundIdMap[nodeDataList.ObserverAssociationAddress] = currentRoundId.Add(1);
            Context.Fire(new ReportProposed
            {
                ObserverAssociationAddress = nodeDataList.ObserverAssociationAddress,
                Report = report
            });
            return report;
        }

        public override Empty ConfirmReport(ConfirmReportInput input)
        {
            // Assert Sender is from certain Observer Association.
            var offChainAggregatorContract = State.OffChainAggregatorContractInfoMap[input.ObserverAssociationAddress];
            if (offChainAggregatorContract == null)
            {
                throw new AssertionException("Observer Association not exists.");
            }

            Assert(offChainAggregatorContract.ObserverList.Value.Contains(Context.Sender),
                "Sender isn't a member of certain Observer Association.");
            State.ObserverSignatureMap[input.ObserverAssociationAddress][input.RoundId][Context.Sender] =
                input.Signature;
            Context.Fire((new ReportConfirmed {RoundId = input.RoundId, Signature = input.Signature}));
            return new Empty();
        }
    }
}