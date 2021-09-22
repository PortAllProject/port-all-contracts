using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public class LatestQueriedReceiptCountProvider : ILatestQueriedReceiptCountProvider, ISingletonDependency
    {
        private readonly Dictionary<string, long> _count = new Dictionary<string, long>();

        public long Get(string symbol)
        {
            if (_count.ContainsKey(symbol)) return _count[symbol];
            _count.Add(symbol, 0);
            return 0;
        }

        public void Set(string symbol, long count)
        {
            _count[symbol] = count;
        }
    }
}