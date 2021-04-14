using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public interface IQueryService
    {
        Task<string> Query(string url, string attribute);
    }

    public class QueryService : IQueryService, ITransientDependency
    {
        public Task<string> Query(string url, string attribute)
        {
            throw new System.NotImplementedException();
        }
    }
}