using System;
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

        protected LogEventProcessorBase(IOptionsSnapshot<AElfContractOptions> contractAddressOptions)
        {
            _contractOptions = contractAddressOptions.Value;
        }

        public abstract string ContractName { get; }

        public abstract Task ProcessAsync(LogEvent logEvent, EventContext context);

        public string GetContractAddress(int chainId)
        {
            if (_contractOptions.ContractAddressList.TryGetValue(ChainHelper.ConvertChainIdToBase58(chainId),
                    out var contractAddresses))
            {
                if (contractAddresses.TryGetValue(ContractName, out var contractAddress))
                {
                    return contractAddress;
                }
            }

            return string.Empty;
        }

        public bool IsMatch(int chainId, string contractAddress, string logEventName)
        {
            var actualContractAddress = GetContractAddress(chainId);
            if (actualContractAddress == string.Empty)
            {
                return false;
            }

            return actualContractAddress == contractAddress && logEventName == typeof(T).Name;
        }
    }
    
    public class EventContext
    {
        public int ChainId { get; set; }
        public long BlockNumber { get; set; }
        public string BlockHash { get; set; }
        public DateTime BlockTime { get; set; }
        public string TransactionId { get; set; }
    }
}