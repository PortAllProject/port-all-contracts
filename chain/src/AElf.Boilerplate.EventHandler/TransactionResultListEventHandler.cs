using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using AElf.WebApp.MessageQueue;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElf.Boilerplate.EventHandler
{
    public class TransactionResultListEventHandler : IDistributedEventHandler<TransactionResultListEto>,
        ISingletonDependency
    {
        private readonly IEnumerable<ILogEventProcessor> _logEventProcessors;
        private readonly ContractAddressOptions _contractAddressOptions;
        private readonly ILogger<TransactionResultListEventHandler> _logger;

        public TransactionResultListEventHandler(IEnumerable<ILogEventProcessor> logEventProcessors,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions,
            ILogger<TransactionResultListEventHandler> logger)
        {
            _logEventProcessors = logEventProcessors;
            _logger = logger;
            _contractAddressOptions = contractAddressOptions.Value;
        }

        public async Task HandleEventAsync(TransactionResultListEto eventData)
        {
            _logger.LogInformation(
                $"Start handling {eventData.TransactionResults.Values.Sum(r => r.Logs.Length)} new event logs of height {eventData.TransactionResults.First().Value.BlockNumber}.");
            foreach (var logEventProcessor in _logEventProcessors)
            {
                foreach (var eventLog in eventData.TransactionResults.Values.SelectMany(result => result.Logs))
                {
                    if (!_contractAddressOptions.ContractAddressMap.TryGetValue(logEventProcessor.ContractName,
                        out var contractAddress)) return;
                    if (_contractAddressOptions.ContractAddressMap.TryGetValue("Consensus", out var consensusAddress) &&
                        eventLog.Address == consensusAddress) return;
                    _logger.LogInformation($"Received event log {eventLog.Name} of contract {eventLog.Address}");
                    if (eventLog.Address != contractAddress) return;
                    if (eventLog.Name != logEventProcessor.LogEventName) return;
                    _logger.LogInformation("Pushing aforementioned event log to processor.");
                    await logEventProcessor.ProcessAsync(new LogEvent
                    {
                        Indexed = {eventLog.Indexed.Select(ByteString.FromBase64)},
                        NonIndexed = ByteString.FromBase64(eventLog.NonIndexed)
                    });
                }
            }
        }
    }
}