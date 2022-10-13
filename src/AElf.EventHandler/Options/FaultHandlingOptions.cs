namespace AElf.EventHandler;

public class FaultHandlingOptions
{
    public bool IsReSendFailedJob { get; set; } = false;
    public string ReSendFailedJobChainId { get; set; }
}