namespace AElf.EventHandler
{
    public interface ILatestQueriedReceiptCountProvider
    {
        long Get();
        void Set(long count);
    }
}