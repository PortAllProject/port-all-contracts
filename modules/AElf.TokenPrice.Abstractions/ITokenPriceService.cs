using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AElf.TokenPrice;

public interface ITokenPriceService
{
    Task<decimal> GetPriceAsync(string symbol);
    Task<decimal> GetHistoryPriceAsync(string symbol, DateTime dateTime);
}

public class TokenPriceService : ITokenPriceService, ITransientDependency
{
    private readonly TokenPriceOptions _options;
    private readonly ITokenPriceProvider _tokenPriceProvider;

    public TokenPriceService(IOptionsSnapshot<TokenPriceOptions> options, ITokenPriceProvider tokenPriceProvider)
    {
        _tokenPriceProvider = tokenPriceProvider;
        _options = options.Value;
    }

    public async Task<decimal> GetPriceAsync(string symbol)
    {
        symbol = symbol.ToUpper();
        var coinId = GetCoinIdAsync(symbol);
        var price = await _tokenPriceProvider.GetPriceAsync(coinId);
        return price;
    }

    public async Task<decimal> GetHistoryPriceAsync(string symbol, DateTime dateTime)
    {
        symbol = symbol.ToUpper();
        var date = dateTime.Date;
        var coinId = GetCoinIdAsync(symbol);
        var price = await _tokenPriceProvider.GetHistoryPriceAsync(coinId, date);
        return price;
    }

    private string GetCoinIdAsync(string symbol)
    {
        var coinId = _options.CoinIdMapping.TryGetValue(symbol, out var id) ? id : null;
        if (coinId.IsNullOrWhiteSpace())
        {
            throw new NotSupportedException($"Do not support symbol: {symbol}");
        }

        return coinId;
    }
}