using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override Report GetReport(Int64Value input)
        {
            return State.ReportMap[input.Value];
        }

        public override StringValue GetSignature(GetSignatureInput input)
        {
            return new StringValue
            {
                Value = State.ObserverSignatureMap[input.RoundNumber][input.Address]
            };
        }
    }
}