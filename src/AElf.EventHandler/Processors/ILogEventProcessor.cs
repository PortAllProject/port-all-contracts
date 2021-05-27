using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;

namespace AElf.EventHandler
{
    public interface ILogEventProcessor<T> where T : IEvent<T>
    {
        string ContractName { get; }
        Task ProcessAsync(LogEvent logEvent);
        bool IsMatch(string contractAddress, string logEventName);
    }
}