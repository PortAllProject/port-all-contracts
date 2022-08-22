namespace AElf.TokenPrice;

public interface ITokenPriceProvider
{
    Task<decimal> GetPriceAsync(string coinId);
    Task<decimal> GetHistoryPriceAsync(string coinId, DateTime dateTime);
}