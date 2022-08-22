using System.Collections.Generic;

namespace AElf.PriceWorker;

public class PriceSyncOptions
{
    public List<string> SourceChains { get; set; } = new();
    public List<string> TargetChains { get; set; } = new();
}