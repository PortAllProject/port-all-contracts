using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public class UrlDataProvider : IDataProvider, ISingletonDependency
    {
        private readonly ILogger<UrlDataProvider> _logger;

        public UrlDataProvider(ILogger<UrlDataProvider> logger)
        {
            _logger = logger;
        }

        public async Task<string> GetDataAsync(Hash queryId, string title = null, List<string> options = null)
        {
            if (title == null || options == null)
            {
                _logger.LogError($"No data of {queryId} for revealing.");
                return string.Empty;
            }

            string result;

            if (!title.Contains('|'))
            {
                result = await GetSingleUrlDataAsync(title, options);
            }
            else
            {
                var urls = title.Split('|');
                var urlAttributes = options.Select(a => a.Split('|')).ToList();
                var dataList = new List<decimal>();
                for (var i = 0; i < urls.Length; i++)
                {
                    var singleData =
                        await GetSingleUrlDataAsync(urls[i], urlAttributes.Select(a => a[i]).ToList());
                    if (singleData.Contains("\""))
                    {
                        singleData = singleData.Replace("\"", "");
                    }

                    if (decimal.TryParse(singleData, out var decimalData))
                    {
                        _logger.LogInformation($"Add {singleData} to data list.");
                        dataList.Add(decimalData);
                    }
                    else
                    {
                        throw new Exception($"Error during paring {singleData} to decimal");
                    }
                }

                result = Aggregate(dataList);
            }

            return result;
        }

        private string Aggregate(List<decimal> dataList)
        {
            var finalPrice = dataList.OrderBy(p => p).ToList()[dataList.Count / 2]
                .ToString(CultureInfo.InvariantCulture);

            _logger.LogInformation($"Final price: {finalPrice}");

            return finalPrice;
        }

        public async Task<string> GetSingleUrlDataAsync(string url, List<string> attributes)
        {
            _logger.LogInformation($"Querying {url} for attributes {attributes.First()} etc..");

            var data = string.Empty;
            var response = string.Empty;
            try
            {
                var client = new HttpClient {Timeout = TimeSpan.FromMinutes(2)};
                using var responseMessage = await client.GetHttpResponseMessageWithRetryAsync(url, _logger);
                response = await responseMessage.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error during querying: {e.Message}");
            }

            try
            {
                _logger.LogInformation($"Trying to parse response to json: {response}");

                if (response != string.Empty)
                {
                    data = JsonHelper.ParseJson(response, attributes);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error during parsing json: {response}\n{e.Message}");
                throw;
            }

            if (string.IsNullOrEmpty(data))
            {
                data = "0";
                _logger.LogError($"Failed to get {attributes.First()} from {response}, will just return 0.");

            }

            return data;
        }
    }
}