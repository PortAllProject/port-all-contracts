using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;
using Common.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public interface ILogEventProcessor<T> : ITransientDependency, ILogEventProcessor
    {

    }

    public interface ILogEventProcessor
    {
        string ContractName { get; }
        Task ProcessAsync(LogEvent logEvent, EventContext context);
        bool IsMatch(int chainId, string contractAddress, string logEventName);
    }
}