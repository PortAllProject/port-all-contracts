namespace AElf.TokenSwap.Dtos
{
    public class TokenSwapInfoDto
    {
        public string BridgeContractBalance { get; set; }
        public string CurrentPeriodStartTimestamp { get; set; }
        public long CurrentPeriodId { get; set; }
        public long CreatedReceiptCount { get; set; }
        public long TransmittedReceiptCount { get; set; }
        public long LotteryCodeCount { get; set; }
        public long VotersCount { get; set; }
        public string VotesCount { get; set; }
    }
}