using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Client.Core.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace AElf.EventHandler;

public class EventHandlerAppHostedService : IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _application;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventHandlerAppHostedService> _logger;

    public EventHandlerAppHostedService(
        IAbpApplicationWithExternalServiceProvider application,
        IServiceProvider serviceProvider, ILogger<EventHandlerAppHostedService> logger)
    {
        _application = application;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _application.Initialize(_serviceProvider);

        var contractAddressOptions =
            _serviceProvider.GetRequiredService<IOptionsSnapshot<AElfContractOptions>>().Value;
        _logger.LogInformation(
            $"AElfContractOptions contains: {contractAddressOptions.ContractAddressList.Keys.Aggregate("", (t, n) => $"{t}\t{n}")}");

        var messageQueueOptions =
            _serviceProvider.GetRequiredService<IOptionsSnapshot<MessageQueueOptions>>().Value;
        _logger.LogInformation($"Message Queue Configs:\n" +
                               $"HostName: {messageQueueOptions.HostName}\n" +
                               $"Uri: {messageQueueOptions.Uri}\n" +
                               $"Port:{messageQueueOptions.Port}\n" +
                               $"ClientName: {messageQueueOptions.ClientName}\n" +
                               $"ExchangeName: {messageQueueOptions.ExchangeName}");

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _application.Shutdown();
        return Task.CompletedTask;
    }
}