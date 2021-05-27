using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;
using AElf.WebApp.MessageQueue;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElf.EventHandler
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
                $"Start handling {eventData.TransactionResults.Values.Sum(r => r.Logs.Length)} new event logs from height {eventData.StartBlockNumber} to {eventData.EndBlockNumber}.\n{GetAllLogEvents(eventData)}");

            var usefulLogEventProcessors = _logEventProcessors.Where(p =>
                _contractAddressOptions.ContractAddressMap.ContainsKey(p.ContractName)).ToList();

            foreach (var eventLog in eventData.TransactionResults.Values.SelectMany(result => result.Logs))
            {
                _logger.LogInformation($"Received event log {eventLog.Name} of contract {eventLog.Address}");
                foreach (var logEventProcessor in usefulLogEventProcessors)
                {
                    if (logEventProcessor.IsMatch(eventLog.Address, eventLog.Name))
                    {
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

        private string GetAllLogEvents(TransactionResultListEto eventData)
        {
            return eventData.TransactionResults.Values.SelectMany(r => r.Logs).Select(l => l.Name)
                .Aggregate("", (c, n) => $"{c}\t{n}");
        }
    }
}