using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public interface IDataProvider
    {
        Task<string> GetDataAsync(Hash queryId, string url = null, string attribute = null);
    }

    public class DataProvider : IDataProvider, ISingletonDependency
    {
        private readonly Dictionary<Hash, string> _dictionary;

        public DataProvider()
        {
            _dictionary = new Dictionary<Hash, string>();
        }

        public async Task<string> GetDataAsync(Hash queryId, string url = null, string attribute = null)
        {
            if (_dictionary.TryGetValue(queryId, out var data))
            {
                return data;
            }

            if (url == null || attribute == null)
            {
                return string.Empty;
            }

            var client = new HttpClient();
            var responseMessage = await client.PostAsync(url, null);
            var response = await responseMessage.Content.ReadAsStringAsync();

            var jsonDoc = JsonDocument.Parse(response);

            if (!jsonDoc.RootElement.TryGetProperty(attribute, out var targetElement)) return string.Empty;

            data = targetElement.GetRawText();
            _dictionary[queryId] = data;

            return data;
        }
    }
}