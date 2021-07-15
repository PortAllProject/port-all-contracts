using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.EventHandler
{
    public class InvalidDataProvider : IDataProvider
    {
        public const string Title = "invalid";
        public Task<string> GetDataAsync(Hash queryId, string title = null, List<string> options = null)
        {
            return Task.FromResult("0");
        }
    }
}