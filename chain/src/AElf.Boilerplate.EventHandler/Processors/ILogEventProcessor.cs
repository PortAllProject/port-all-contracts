using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Boilerplate.EventHandler
{
    public interface ILogEventProcessor
    {
        string ContractName { get; }
        string LogEventName { get; }
        Task ProcessAsync(LogEvent logEvent);
        string GetContractAddress();
        bool IsMatch(string contractAddress, string logEventName);
    }
}