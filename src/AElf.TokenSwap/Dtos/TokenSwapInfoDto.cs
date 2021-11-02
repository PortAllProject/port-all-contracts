namespace AElf.TokenSwap.Dtos
{
    public class TokenSwapInfoDto
    {
        public double BridgeContractBalance { get; set; }
        public long CreatedReceiptCount { get; set; }
        public long TransmittedReceiptCount { get; set; }
        public long VotersCount { get; set; }
        public double VotesCount { get; set; }
    }
}