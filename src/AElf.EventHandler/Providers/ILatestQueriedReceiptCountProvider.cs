namespace AElf.EventHandler;

public interface ILatestQueriedReceiptCountProvider
{
    long Get(string swapId);
    void Set(string swapId, long count);
}