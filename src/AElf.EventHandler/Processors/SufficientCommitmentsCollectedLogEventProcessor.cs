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
    private readonly AElfContractOptions _contractAddressOptions;
    private readonly ILogger<SufficientCommitmentsCollectedLogEventProcessor> _logger;
    private readonly OracleOptions _oracleOptions;
    private readonly IOracleService _oracleService;
    private readonly AElfChainAliasOptions _aelfChainAliasOptions;

    public SufficientCommitmentsCollectedLogEventProcessor(
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        ISaltProvider saltProvider, IDataProvider dataProvider,
        ILogger<SufficientCommitmentsCollectedLogEventProcessor> logger,
        IOptionsSnapshot<OracleOptions> oracleOptions,
        IOracleService oracleService,
        IOptionsSnapshot<AElfChainAliasOptions> aelfChainAliasOptions) : base(contractAddressOptions)
    {
        _saltProvider = saltProvider;
        _dataProvider = dataProvider;
        _logger = logger;
        _contractAddressOptions = contractAddressOptions.Value;
        _oracleOptions = oracleOptions.Value;
        _oracleService = oracleService;
        _aelfChainAliasOptions = aelfChainAliasOptions.Value;
    }

    public override string ContractName => "Oracle";

    public override async Task ProcessAsync(LogEvent logEvent, EventContext context)
    {
        var collected = new SufficientCommitmentsCollected();
        collected.MergeFrom(logEvent);
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
            Salt = _saltProvider.GetSalt(context.ChainId.ToString(),collected.QueryId)
        };
        _logger.LogInformation($"Sending Reveal tx with input: {revealInput}");
        var transaction = await _oracleService.RevealAsync(_aelfChainAliasOptions.Mapping[context.ChainId.ToString()],revealInput);
        _logger.LogInformation($"[Reveal] Transaction : {transaction}");
    }
}