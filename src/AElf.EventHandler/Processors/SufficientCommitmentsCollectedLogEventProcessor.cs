using System.Threading.Tasks;
using AElf.Client.Core.Options;
using AElf.Contracts.Oracle;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.EventHandler;

internal class SufficientCommitmentsCollectedLogEventProcessor :
    LogEventProcessorBase<SufficientCommitmentsCollected>
{
    private readonly ISaltProvider _saltProvider;
    private readonly IDataProvider _dataProvider;
    private readonly AElfContractOptions _contractAddressOptions;
    private readonly ILogger<SufficientCommitmentsCollectedLogEventProcessor> _logger;

    public SufficientCommitmentsCollectedLogEventProcessor(
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        ISaltProvider saltProvider, IDataProvider dataProvider,
        ILogger<SufficientCommitmentsCollectedLogEventProcessor> logger) : base(contractAddressOptions)
    {
        _saltProvider = saltProvider;
        _dataProvider = dataProvider;
        _logger = logger;
        _contractAddressOptions = contractAddressOptions.Value;
    }

    public override string ContractName => "Oracle";

    public override async Task ProcessAsync(LogEvent logEvent)
    {
        // var collected = new SufficientCommitmentsCollected();
        // collected.MergeFrom(logEvent);
        // var data = await _dataProvider.GetDataAsync(collected.QueryId);
        // if (string.IsNullOrEmpty(data))
        // {
        //     _logger.LogError($"Failed to reveal data for query {collected.QueryId}.");
        //     return;
        // }
        //
        // _logger.LogInformation($"Get data for revealing: {data}");
        // var node = new NodeManager(_configOptions.BlockChainEndpoint, _configOptions.AccountAddress,
        //     _configOptions.AccountPassword);
        // var revealInput = new RevealInput
        // {
        //     QueryId = collected.QueryId,
        //     Data = data,
        //     Salt = _saltProvider.GetSalt(collected.QueryId)
        // };
        // _logger.LogInformation($"Sending Reveal tx with input: {revealInput}");
        // var txId = node.SendTransaction(_configOptions.AccountAddress,
        //     _contractAddressOptions.ContractAddressList[ContractName], "Reveal", revealInput);
        // _logger.LogInformation($"[Reveal] Tx id {txId}");
    }
}