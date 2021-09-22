namespace AElf.EventHandler
{
    public interface ILatestQueriedReceiptCountProvider
    {
        long Get(string symbol);
        void Set(string symbol, long count);
    }
}