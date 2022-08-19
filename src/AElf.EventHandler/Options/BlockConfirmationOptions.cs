using System.Collections.Generic;

namespace AElf.EventHandler;

public class BlockConfirmationOptions
{
    public Dictionary<string, long> ConfirmationCount { get; set; } = new();
}