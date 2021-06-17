using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override Report GetReport(GetReportInput input)
        {
            return State.ReportMap[input.Token][input.RoundId];
        }

        public override StringValue GetSignature(GetSignatureInput input)
        {
            var signature = State.ObserverSignatureMap[input.Token][input.RoundId][input.Address];
            return new StringValue
            {
                Value = signature ?? string.Empty
            };
        }

        public override OffChainAggregationInfo GetOffChainAggregationInfo(StringValue input)
        {
            return State.OffChainAggregationInfoMap[input.Value];
        }

        public override ReportQueryRecord GetReportQueryRecord(Hash input)
        {
            return State.ReportQueryRecordMap[input];
        }

        public override MerklePath GetMerklePath(GetMerklePathInput input)
        {
            var merkleTree = State.BinaryMerkleTreeMap[input.Token][input.RoundId];
            Assert(merkleTree.LeafCount > input.NodeIndex, "Node index exceeded.");
            return merkleTree.GenerateMerklePath(input.NodeIndex);
        }

        public override Int64Value GetCurrentRoundId(StringValue input)
        {
            var roundId = State.CurrentRoundIdMap[input.Value];
            return new Int64Value
            {
                Value = roundId
            };
        }

        public override StringValue GenerateRawReport(GenerateRawReportInput input)
        {
            var report = GenerateRawReport(input.ConfigDigest, input.Organization, input.Report);
            return new StringValue
            {
                Value = report
            };
        }

        public override StringValue GetRawReport(GetRawReportInput input)
        {
            var offChainAggregationInfo = State.OffChainAggregationInfoMap[input.Token];
            if (offChainAggregationInfo == null)
            {
                throw new AssertionException($"token: [{input.Token}] info does not exist");
            }

            var roundReport = State.ReportMap[input.Token][input.RoundId];
            Assert(roundReport != null,
                $"contract: [{input.Token}]: round: [{input.RoundId}] info does not exist");
            var configDigest = offChainAggregationInfo.ConfigDigest;
            var organization = offChainAggregationInfo.RegimentAddress;
            var report = GenerateRawReport(configDigest, organization, roundReport);
            return new StringValue
            {
                Value = report
            };
        }

        public override SignatureMap GetSignatureMap(GetSignatureMapInput input)
        {
            var offChainAggregationInfo = State.OffChainAggregationInfoMap[input.Token];
            if (offChainAggregationInfo == null)
            {
                throw new AssertionException("Report not exists.");
            }

            var signatureMap = new SignatureMap();
            foreach (var observer in State.ObserverListMap[offChainAggregationInfo.RegimentAddress].Value)
            {
                var signature = State.ObserverSignatureMap[input.Token][input.RoundId][observer];
                if (signature != null)
                {
                    signatureMap.Value[observer.ToBase58()] = signature;
                }
            }

            return signatureMap;
        }

        public override BoolValue IsInRegisterWhiteList(Address input)
        {
            return new BoolValue
            {
                Value = State.RegisterWhiteListMap[input]
            };
        }

        public override ObserverList GetObserverList(Address input)
        {
            return State.ObserverListMap[input];
        }

        public override BoolValue IsObserver(IsObserverInput input)
        {
            return new BoolValue
            {
                Value = State.ObserverListMap[input.RegimentAddress].Value.Contains(input.OracleNodeAddress)
            };
        }
    }
}