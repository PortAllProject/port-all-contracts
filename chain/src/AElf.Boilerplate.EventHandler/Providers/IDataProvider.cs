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

            data = string.Empty;
            if (url == null || attributes == null)
            {
                return string.Empty;
            }

            _logger.LogCritical($"Querying {url} for attribute {attributes.First()} etc..");

            var client = new HttpClient();
            var responseMessage = await client.PostAsync(url, null);
            var response = await responseMessage.Content.ReadAsStringAsync();

            var jsonDoc = JsonDocument.Parse(response);

            foreach (var attribute in attributes)
            {
                if (jsonDoc.RootElement.TryGetProperty(attribute, out var targetElement))
                {
                    data += targetElement.GetRawText();
                    _dictionary[queryId] = data;
                }
                else
                {
                    return data;
                }
            }

            return data;
        }
    }
}