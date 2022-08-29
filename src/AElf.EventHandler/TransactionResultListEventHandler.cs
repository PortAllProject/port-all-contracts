using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Core.Options;
using AElf.Types;
using AElf.WebApp.MessageQueue;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace AElf.EventHandler;

public class TransactionResultListEventHandler : IDistributedEventHandler<TransactionResultListEto>,
    ISingletonDependency
{
    private readonly IEnumerable<ILogEventProcessor> _logEventProcessors;
    private readonly AElfContractOptions _contractAddressOptions;
    private readonly ILogger<TransactionResultListEventHandler> _logger;
    private readonly IChainIdProvider _chainIdProvider;

    public TransactionResultListEventHandler(IEnumerable<ILogEventProcessor> logEventProcessors,
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        ILogger<TransactionResultListEventHandler> logger, IChainIdProvider chainIdProvider)
    {
        _logEventProcessors = logEventProcessors;
        _logger = logger;
        _chainIdProvider = chainIdProvider;
        _contractAddressOptions = contractAddressOptions.Value;
    }

    public async Task HandleEventAsync(TransactionResultListEto eventData)
    {
        var chainId = _chainIdProvider.GetChainId(eventData.ChainId);
        if (!_contractAddressOptions.ContractAddressList.TryGetValue(chainId, out var contractAddresses))
        {
            return;
        }

        var usefulLogEventProcessors = _logEventProcessors.Where(p =>
            contractAddresses.ContainsKey(p.ContractName)).ToList();

        foreach (var txResultEto in eventData.TransactionResults.Values.SelectMany(result => result))
        {
            foreach (var eventLog in txResultEto.Logs)
            {
                //_logger.LogInformation($"Received event log {eventLog.Name} of contract {eventLog.Address}");
                foreach (var logEventProcessor in usefulLogEventProcessors)
                {
                    if (logEventProcessor.IsMatch(eventData.ChainId, eventLog.Address, eventLog.Name))
                    {
                        //_logger.LogInformation("Pushing aforementioned event log to processor.");
                        await logEventProcessor.ProcessAsync(new LogEvent
                        {
                            Indexed = { eventLog.Indexed.Select(ByteString.FromBase64) },
                            NonIndexed = ByteString.FromBase64(eventLog.NonIndexed)
                        }, new EventContext
                        {
                            ChainId = eventData.ChainId,
                            BlockNumber = txResultEto.BlockNumber,
                            BlockHash = txResultEto.BlockHash,
                            BlockTime = txResultEto.BlockTime,
                            TransactionId = txResultEto.TransactionId
                        });
                    }
                }
            }
        }
    }
}