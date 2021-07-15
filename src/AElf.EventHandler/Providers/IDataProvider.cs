using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.EventHandler
{
    public interface IDataProvider
    {
        Task<string> GetDataAsync(Hash queryId, string title = null, List<string> options = null);
    }
}