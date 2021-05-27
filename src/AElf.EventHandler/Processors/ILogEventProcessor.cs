using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;
using Common.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public interface ILogEventProcessor<T> : ITransientDependency, ILogEventProcessor where T : IEvent<T>
    {

    }

    public interface ILogEventProcessor
    {
        string ContractName { get; }
        Task ProcessAsync(LogEvent logEvent);
        bool IsMatch(string contractAddress, string logEventName);
    }
}