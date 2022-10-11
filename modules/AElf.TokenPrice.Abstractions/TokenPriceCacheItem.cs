namespace AElf.TokenPrice;

[Serializable]
public class TokenPriceCacheItem
{
    public string Symbol { get; set; }
    public string CoinId { get; set; }
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}