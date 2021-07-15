using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    /// <summary>
    /// Query title and save latest result.
    /// </summary>
    public interface IQueryService
    {
        Task<string> GetDataAsync(Hash queryId, string title = null, List<string> options = null);
    }

    public class QueryService : IQueryService, ISingletonDependency
    {
        private readonly IDataProviderSelector _dataProviderSelector;
        private readonly ILogger<QueryService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly Dictionary<Hash, string> _dictionary;

        public QueryService(ILogger<QueryService> logger, IDataProviderSelector dataProviderSelector,
            IServiceScopeFactory serviceScopeFactory)
        {
            _dataProviderSelector = dataProviderSelector;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
            _dictionary = new Dictionary<Hash, string>();
        }

        public async Task<string> GetDataAsync(Hash queryId, string title = null, List<string> options = null)
        {
            if (_dictionary.TryGetValue(queryId, out var data))
            {
                return data;
            }

            var dataProviderType = _dataProviderSelector.Select(title);
            using var scope = _serviceScopeFactory.CreateScope();
            var dataProvider = (IDataProvider) scope.ServiceProvider.GetRequiredService(dataProviderType);
            data = await dataProvider.GetDataAsync(queryId, title, options);
            _dictionary[queryId] = data;
            return data;
        }
    }
}