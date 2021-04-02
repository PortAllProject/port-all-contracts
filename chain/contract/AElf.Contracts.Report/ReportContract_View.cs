using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override Report GetReport(GetReportInput input)
        {
            return State.ReportMap[input.EthereumContractAddress][input.RoundId];
        }

        public override StringValue GetSignature(GetSignatureInput input)
        {
            return new StringValue
            {
                Value = State.ObserverSignatureMap[input.EthereumContractAddress][input.RoundId][input.Address]
            };
        }

        public override OffChainAggregatorContractInfo GetOffChainAggregatorContractInfo(StringValue input)
        {
            return State.OffChainAggregatorContractInfoMap[input.Value];
        }

        public override ReportQueryRecord GetReportQueryRecord(Hash input)
        {
            return State.ReportQueryRecordMap[input];
        }

        public override MerklePath GetMerklePath(GetMerklePathInput input)
        {
            return State.BinaryMerkleTreeMap[input.EthereumContractAddress][input.RoundId]
                .GenerateMerklePath(input.NodeIndex);
        }
    }
}