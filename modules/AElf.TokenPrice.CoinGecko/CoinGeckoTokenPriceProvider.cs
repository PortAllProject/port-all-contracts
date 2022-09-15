using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using CoinGecko.Clients;
using CoinGecko.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.TokenPrice.CoinGecko
{
    public class CoinGeckoTokenPriceProvider : ITokenPriceProvider, ITransientDependency
    {
        private readonly ICoinGeckoClient _coinGeckoClient;
        private readonly IRequestLimitProvider _requestLimitProvider;

        private const string UsdSymbol = "usd";

        public ILogger<CoinGeckoTokenPriceProvider> Logger { get; set; }

        public CoinGeckoTokenPriceProvider(IRequestLimitProvider requestLimitProvider)
        {
            _requestLimitProvider = requestLimitProvider;
            _coinGeckoClient = CoinGeckoClient.Instance;

            Logger = NullLogger<CoinGeckoTokenPriceProvider>.Instance;
        }
        
        public async Task<decimal> GetPriceAsync(string coinId)
        {
            try
            {
                var coinData =
                    await RequestAsync(async () =>
                        await _coinGeckoClient.SimpleClient.GetSimplePrice(new[] {coinId}, new[] { UsdSymbol }));

                if (!coinData.TryGetValue(coinId,out var value))
                {
                    return 0;
                }

                return value[UsdSymbol].Value;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"can not get current price :{coinId}.");
                throw;
            }
        }

        public async Task<decimal> GetHistoryPriceAsync(string coinId, DateTime dateTime)
        {
            try
            {
                // var proxy = new WebProxy
                // {
                //     Address = new Uri("http://127.0.0.1:1087"),
                // };
                // var clientHandler = new HttpClientHandler()
                // {
                //     Proxy = proxy,
                // };
                // var client = new CoinGeckoClient(clientHandler);
                
                var coinData =
                    await RequestAsync(async () => await _coinGeckoClient.CoinsClient.GetHistoryByCoinId(coinId,
                        dateTime.ToString("dd-MM-yyyy"), "false"));

                if (coinData.MarketData == null)
                {
                    return 0;
                }

                return (decimal) coinData.MarketData.CurrentPrice[UsdSymbol].Value;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"can not get :{coinId} price.");
                throw;
            }
        }

        private async Task<T> RequestAsync<T>(Func<Task<T>> task)
        {
            await _requestLimitProvider.RecordRequestAsync();
            return await task();
        }
    }
}