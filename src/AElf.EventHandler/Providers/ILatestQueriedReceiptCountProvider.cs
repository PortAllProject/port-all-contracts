using System;

namespace AElf.EventHandler;

public interface ILatestQueriedReceiptCountProvider
{
    long Get(string swapId);
    void Set(DateTime time,string swapId, long count);
}