using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Core.Extensions;
using AElf.Client.Core.Options;
using AElf.Client.Oracle;
using AElf.Contracts.Oracle;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler;

internal class QueryCreatedLogEventProcessor : LogEventProcessorBase<QueryCreated>, ISingletonDependency
{
    private readonly ISaltProvider _saltProvider;
    private readonly IDataProvider _dataProvider;
    private readonly BridgeOptions _bridgeOptions;
    private readonly OracleOptions _oracleOptions;
    private readonly IOracleService _oracleService;

    public override string ContractName => "OracleContract";
    private readonly ILogger<QueryCreatedLogEventProcessor> _logger;

    public QueryCreatedLogEventProcessor(
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        ISaltProvider saltProvider, 
        IDataProvider dataProvider, 
        ILogger<QueryCreatedLogEventProcessor> logger,
        IOptionsSnapshot<BridgeOptions> bridgeOptions,
        IOptionsSnapshot<OracleOptions> oracleOptions,
        IOracleService oracleService) :
        base(contractAddressOptions)
    {
        _saltProvider = saltProvider;
        _dataProvider = dataProvider;
        _logger = logger;
        _bridgeOptions = bridgeOptions.Value;
        _oracleOptions = oracleOptions.Value;
        _oracleService = oracleService;
    }

    public override async Task ProcessAsync(LogEvent logEvent, EventContext context)
    {
        var queryCreated = new QueryCreated();
        queryCreated.MergeFrom(logEvent);
        _logger.LogInformation(queryCreated.ToString());

        var chainId = ChainIdProvider.GetChainId(context.ChainId);
        
        var nodeAddress = Address.FromBase58(_bridgeOptions.AccountAddress);
        var firstDesignatedNodeAddress = queryCreated.DesignatedNodeList.Value.First();
        //var queryToken = queryCreated.Token; // Query token means the ethereum contract address oracle node should cares in report case.
        if (queryCreated.DesignatedNodeList.Value.Contains(nodeAddress) ||
            _oracleOptions.ObserverAssociationAddressList.Contains(firstDesignatedNodeAddress.ToBase58()))
        {
            var data = await _dataProvider.GetDataAsync(queryCreated.QueryId, queryCreated.QueryInfo.Title,
                queryCreated.QueryInfo.Options.ToList());
            if (string.IsNullOrEmpty(data))
            {
                var swapId = queryCreated.QueryInfo.Title.Split("_").Last();
                _logger.LogError(queryCreated.QueryInfo.Title == "record_receipts"
                    ? $"Failed to record receipts. Swap Id :{swapId}"
                    : $"Failed to response to query {queryCreated.QueryId}.");
        
                return;
            }
        
            var salt = _saltProvider.GetSalt(chainId, queryCreated.QueryId);
            _logger.LogInformation($"Queried data: {data}, salt: {salt}");
            var commitInput = new CommitInput
            {
                QueryId = queryCreated.QueryId,
                Commitment = HashHelper.ConcatAndCompute(
                    HashHelper.ComputeFrom(data),
                    HashHelper.ConcatAndCompute(salt, HashHelper.ComputeFrom(_bridgeOptions.AccountAddress)))
            };
            _logger.LogInformation($"Sending Commit tx with input: {commitInput}");
            var transactionResult = await _oracleService.CommitAsync(chainId, commitInput);
            _logger.LogInformation($"[Commit] Transaction id {transactionResult.TransactionResult.TransactionId}");
        }
    }
}