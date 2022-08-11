using System.Threading.Tasks;
using AElf.Client.Core.Options;
using AElf.CSharp.Core;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public abstract class LogEventProcessorBase<T> : ILogEventProcessor<T>
    {
        private readonly AElfContractOptions _contractOptions;

        public LogEventProcessorBase(IOptionsSnapshot<AElfContractOptions> contractAddressOptions)
        {
            _contractOptions = contractAddressOptions.Value;
        }

        public abstract string ContractName { get; }

        public abstract Task ProcessAsync(LogEvent logEvent);

        public string GetContractAddress()
        {
            return _contractOptions.ContractAddressList.TryGetValue(ContractName, out var contractAddress)
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