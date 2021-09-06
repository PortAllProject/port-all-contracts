namespace AElf.TokenSwap.Dtos
{
    public class TokenSwapInfoDto
    {
        public long BridgeContractBalance { get; set; }
        public string PreviousPeriodAwardIds { get; set; }
        public string CurrentPeriodAwardIds { get; set; }
        public string PreviousPeriodEndTimestamp { get; set; }
        public string CurrentPeriodEndTimestamp { get; set; }
        public long PreviousPeriodId { get; set; }
        public long CurrentPeriodId { get; set; }
        public long CreatedReceiptCount { get; set; }
        public long TransmittedReceiptCount { get; set; }
    }
}