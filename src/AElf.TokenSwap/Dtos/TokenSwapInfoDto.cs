namespace AElf.TokenSwap.Dtos
{
    public class TokenSwapInfoDto
    {
        public long BridgeContractBalance { get; set; }
        public long PreviousPeriodAward { get; set; }
        public long CreatedReceiptCount { get; set; }
        public long TransmittedReceiptCount { get; set; }
    }
}