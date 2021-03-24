using System.Linq;
using AElf.Contracts.Association;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract : ReportContractContainer.ReportContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            State.OracleContract.Value = input.TokenContractAddress;
            State.OracleTokenSymbol.Value = State.OracleContract.GetOracleTokenSymbol.Call(new Empty()).Value;
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.AssociationContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.AssociationContractSystemName);
            State.ReportFee.Value = input.ReportFee == 0 ? DefaultReportFee : input.ReportFee;
            State.ApplyObserverFee.Value =
                input.ApplyObserverFee == 0 ? DefaultApplyObserverFee : input.ApplyObserverFee;
            State.CurrentReportNumber.Value = 1;
            State.CurrentEpochNumber.Value = 1;
            State.CurrentRoundNumber.Value = 1;
            CreateObserverAssociation(input.InitialObserverList);
            return new Empty();
        }

        private void CreateObserverAssociation(ObserverList initialObserverList)
        {
            State.AssociationContract.CreateOrganization.Send(new CreateOrganizationInput
            {
                CreationToken = HashHelper.ComputeFrom(Context.Self),
                OrganizationMemberList = new OrganizationMemberList {OrganizationMembers = {initialObserverList.Value}},
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 5,
                    MinimalVoteThreshold = 5
                },
                ProposerWhiteList = new ProposerWhiteList {Proposers = {Context.Self}}
            });
        }

        public override Hash QueryOracle(QueryOracleInput input)
        {
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
                AggregateThreshold = input.AggregateThreshold,
                AggregatorContractAddress = input.AggregatorContractAddress,
                UrlToQuery = input.UrlToQuery,
                AttributeToFetch = input.AttributeToFetch,
                DesignatedNodeList = new AddressList {Value = {input.DesignatedNodes}},
                QueryManager = Context.Self,
                CallbackInfo = new CallbackInfo
                {
                    ContractAddress = Context.Self,
                    MethodName = nameof(AppendQueryToReport)
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

        // TODO: Remove
        public override Empty AppendQueryToReport(CallbackInput input)
        {
            // Only Oracle Contract can append query to report.
            Assert(Context.Sender == State.OracleContract.Value, "No permission.");
            // Check whether this Query is delegated by Report Contract (which means this Query already paid report fee).
            var isDelegated = State.ReportQueryRecordMap[input.QueryId] != null;
            if (!isDelegated)
            {
                var queryRecord = State.OracleContract.GetQueryRecord.Call(input.QueryId);
                var queryManager = queryRecord.QueryManager;
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = queryManager,
                    To = Context.Self,
                    Amount = State.ReportFee.Value,
                    Symbol = State.OracleTokenSymbol.Value
                });
            }

            return new Empty();
        }

        public override Report ProposeReport(CallbackInput input)
        {
            Assert(Context.Sender == State.OracleContract.Value, "Only Oracle Contract can propose report after aggregation.");

            return new Report
            {
                EpochNumber = State.CurrentEpochNumber.Value,
                RoundNumber = State.CurrentRoundNumber.Value,
                
            };
        }

        private void AssertSenderIsEpochLeader()
        {
            var currentEpoch = State.EpochMap[State.CurrentEpochNumber.Value];
            var leader = currentEpoch.Observers.FirstOrDefault(o => o.IsLeader);
            Assert(leader != null && leader.Address == Context.Sender, "Sender isn't epoch leader.");
        }
        
        public override Empty ConfirmReport(ConfirmReportInput input)
        {
            Assert(State.ObserverList.Value.Value.Contains(Context.Sender), "No permission.");
            State.ObserverSignatureMap[input.ReportId][Context.Sender] = input.Signature;
            return new Empty();
        }
    }
}