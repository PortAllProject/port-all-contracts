using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public abstract class LogEventProcessorBase<T> : ILogEventProcessor<T> where T : IEvent<T>
    {
        private readonly ContractAddressOptions _contractAddressOptions;

        public LogEventProcessorBase(IOptionsSnapshot<ContractAddressOptions> contractAddressOptions)
        {
            _contractAddressOptions = contractAddressOptions.Value;
        }

        public abstract string ContractName { get; }

        public abstract Task ProcessAsync(LogEvent logEvent);

        public string GetContractAddress()
        {
            return _contractAddressOptions.ContractAddressMap.TryGetValue(ContractName, out var contractAddress)
                ? contractAddress
                : string.Empty;
        }

        public bool IsMatch(string contractAddress, string logEventName)
        {
            var actualContractAddress = GetContractAddress();
            if (actualContractAddress == string.Empty)
            {
                return false;
            }

            return actualContractAddress == contractAddress && logEventName == typeof(T).Name;
        }
    }
}