namespace AElf.Contracts.Report
{
    public partial class ReportContract
    {
        private Report GetCurrentReport() => State.ReportMap[State.CurrentReportNumber.Value];
        private Epoch GetCurrentEpoch() => State.EpochMap[State.CurrentEpochNumber.Value];

    }
}