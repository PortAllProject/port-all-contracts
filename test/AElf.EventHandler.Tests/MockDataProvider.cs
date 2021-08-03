using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler.Tests
{
    public class MockDataProvider : IDataProvider, ISingletonDependency
    {
        public const string Title = "test";
        public Task<string> GetDataAsync(Hash queryId, string title = null, List<string> options = null)
        {
            return Task.FromResult("test");
        }
    }
}