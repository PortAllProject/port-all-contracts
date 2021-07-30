using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public class LatestQueriedReceiptCountProvider : ILatestQueriedReceiptCountProvider, ISingletonDependency
    {
        private long _count;

        public long Get()
        {
            return _count;
        }

        public void Set(long count)
        {
            _count = count;
        }
    }
}