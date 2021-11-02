using System.Collections.Generic;

namespace AElf.TokenSwap.Dtos
{
    public class TokenSwapInfoDto
    {
        public List<double> BridgeContractBalances { get; set; }
        public List<long> CreatedReceiptCounts { get; set; }
        public List<long> TransmittedReceiptCounts { get; set; }
        public long VotersCount { get; set; }
        public double VotesCount { get; set; }
    }
}