using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override Report GetReport(Hash input)
        {
            return State.ReportMap[input];
        }

        public override BytesValue GetRawReport(Hash input)
        {
            return base.GetRawReport(input);
        }
    }
}