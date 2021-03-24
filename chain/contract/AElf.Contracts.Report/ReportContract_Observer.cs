using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        public override Empty ApplyObserver(ApplyObserverInput input)
        {
            return new Empty();
        }

        public override Empty QuitObserver(QuitObserverInput input)
        {
            return new Empty();
        }
    }
}