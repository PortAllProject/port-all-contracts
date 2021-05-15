using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public interface IDataProvider
    {
        Task<string> GetDataAsync(Hash queryId, string url = null, List<string> attributes = null);
    }

    public class DataProvider : IDataProvider, ISingletonDependency
    {
        private readonly Dictionary<Hash, string> _dictionary;
        private readonly ILogger<DataProvider> _logger;

        public DataProvider(ILogger<DataProvider> logger)
        {
            _logger = logger;
            _dictionary = new Dictionary<Hash, string>();
        }

        public async Task<string> GetDataAsync(Hash queryId, string url = null, List<string> attributes = null)
        {
            if (_dictionary.TryGetValue(queryId, out var data))
            {
                return data;
            }

            if (url == null)
            {
                _logger.LogError($"No data of {queryId} for revealing.");
                return string.Empty;
            }

            if (!url.Contains('|')) return await GetSingleUrlDataAsync(url, attributes);
            var urls = url.Split('|');
            var urlAttributes = attributes.Select(a => a.Split('|')).ToList();
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

            return Aggregate(dataList, queryId);
        }

        private string Aggregate(List<decimal> dataList, Hash queryId)
        {
            var finalPrice = dataList.OrderBy(p => p).ToList()[dataList.Count / 2]
                .ToString(CultureInfo.InvariantCulture);

            _dictionary[queryId] = finalPrice;
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
                var client = new HttpClient();
                var responseMessage = await client.GetAsync(url);
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
                    data = ParseJson(response, attributes);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error during parsing json: {response}\n{e.Message}");
                throw;
            }

            return data;
        }

        private string ParseJson(string response, List<string> attributes)
        {
            var jsonDoc = JsonDocument.Parse(response);
            var data = string.Empty;

            foreach (var attribute in attributes)
            {
                if (!attribute.Contains('/'))
                {
                    if (jsonDoc.RootElement.TryGetProperty(attribute, out var targetElement))
                    {
                        if (data == string.Empty)
                        {
                            data = targetElement.GetRawText();
                        }
                        else
                        {
                            data += $";{targetElement.GetRawText()}";
                        }
                    }
                    else
                    {
                        return data;
                    }
                }
                else
                {
                    var attrs = attribute.Split('/');
                    var targetElement = jsonDoc.RootElement.GetProperty(attrs[0]);
                    foreach (var attr in attrs.Skip(1))
                    {
                        if (!targetElement.TryGetProperty(attr, out targetElement))
                        {
                            return attr;
                        }
                    }

                    if (data == string.Empty)
                    {
                        data = targetElement.GetRawText();
                    }
                    else
                    {
                        data += $";{targetElement.GetRawText()}";
                    }
                }
            }

            return data;
        }
    }
}