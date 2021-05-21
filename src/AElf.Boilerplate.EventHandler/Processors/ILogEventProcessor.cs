using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Boilerplate.EventHandler
{
    public interface ILogEventProcessor
    {
        string ContractName { get; }
        Task ProcessAsync(LogEvent logEvent);
        bool IsMatch(string contractAddress, string logEventName);
    }
}