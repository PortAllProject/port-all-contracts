using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override Report GetReport(GetReportInput input)
        {
            return State.ReportMap[input.ObserverAssociationAddress][input.RoundId];
        }

        public override StringValue GetSignature(GetSignatureInput input)
        {
            return new StringValue
            {
                Value = State.ObserverSignatureMap[input.ObserverAssociationAddress][input.RoundId][input.Address]
            };
        }

        public override OffChainAggregatorContractInfo GetOffChainAggregatorContractInfo(Address input)
        {
            return State.OffChainAggregatorContractInfoMap[input];
        }

        public override ReportQueryRecord GetReportQueryRecord(Hash input)
        {
            return State.ReportQueryRecordMap[input];
        }

        public override MerklePath GetMerklePath(GetMerklePathInput input)
        {
            return State.BinaryMerkleTreeMap[input.ObserverAssociationAddress][input.RoundId]
                .GenerateMerklePath(input.NodeIndex);
        }
    }
}