using System.Collections.Generic;
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

            if (url == null || attributes == null)
            {
                return string.Empty;
            }

            _logger.LogCritical($"Querying {url} for attribute {attributes.First()} etc..");

            var client = new HttpClient();
            var responseMessage = await client.PostAsync(url, null);
            var response = await responseMessage.Content.ReadAsStringAsync();
            data = ParseJson(response, attributes);
            _dictionary[queryId] = data;
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