using System.Collections.Generic;

namespace AElf.TokenPrice
{
    public class TokenPriceOptions
    {
        public Dictionary<string, string> CoinIdMapping { get; set; } = new();
    }
}