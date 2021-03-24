using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
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
            State.ReportFee.Value = input.ReportFee;
            return new Empty();
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
            State.OriginQueryManagerMap[queryId] = Context.Sender;
            return queryId;
        }

        public override Empty CancelQueryOracle(Hash input)
        {
            Assert(State.OriginQueryManagerMap[input] == Context.Sender, "No permission.");
            State.OracleContract.CancelQuery.Send(input);
            return new Empty();
        }

        public override Empty AppendQueryToReport(CallbackInput input)
        {
            return new Empty();
        }

        public override Empty CommitSignature(CommitSignatureInput input)
        {
            return new Empty();
        }
    }
}