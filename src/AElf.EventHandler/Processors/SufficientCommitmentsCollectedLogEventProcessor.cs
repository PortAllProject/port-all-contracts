using System.Threading.Tasks;
using AElf.Client.Core.Extensions;
using AElf.Client.Core.Options;
using AElf.Client.Oracle;
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
    private readonly ILogger<SufficientCommitmentsCollectedLogEventProcessor> _logger;
    private readonly IOracleService _oracleService;

    public SufficientCommitmentsCollectedLogEventProcessor(
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        ISaltProvider saltProvider, IDataProvider dataProvider,
        ILogger<SufficientCommitmentsCollectedLogEventProcessor> logger,
        IOracleService oracleService) : base(contractAddressOptions)
    {
        _saltProvider = saltProvider;
        _dataProvider = dataProvider;
        _logger = logger;
        _oracleService = oracleService;
    }

    public override string ContractName => "OracleContract";

    public override async Task ProcessAsync(LogEvent logEvent, EventContext context)
    {
        var collected = new SufficientCommitmentsCollected();
        collected.MergeFrom(logEvent);

        var chainId = ChainIdProvider.GetChainId(context.ChainId);
        var data = await _dataProvider.GetDataAsync(collected.QueryId);
        if (string.IsNullOrEmpty(data))
        {
            _logger.LogError($"Failed to reveal data for query {collected.QueryId}.");
            return;
        }
        
        _logger.LogInformation($"Get data for revealing: {data}");
        var revealInput = new RevealInput
        {
            QueryId = collected.QueryId,
            Data = data,
            Salt = _saltProvider.GetSalt(chainId,collected.QueryId)
        };
        _logger.LogInformation($"Sending Reveal tx with input: {revealInput}");
        var transaction = await _oracleService.RevealAsync(chainId,revealInput);
        _logger.LogInformation($"[Reveal] Transaction id  : {transaction.TransactionResult.TransactionId}");
    }
}