using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Oracle;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public class QueryCreatedLogEventProcessor : LogEventProcessorBase, ISingletonDependency
    {
        private readonly ISaltProvider _saltProvider;
        private readonly IDataProvider _dataProvider;
        private readonly ConfigOptions _configOptions;
        public override string ContractName => "Oracle";
        public override string LogEventName => nameof(QueryCreated);
        private readonly ILogger<QueryCreatedLogEventProcessor> _logger;

        public QueryCreatedLogEventProcessor(IOptionsSnapshot<ConfigOptions> configOptions,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions,
            ISaltProvider saltProvider, IDataProvider dataProvider, ILogger<QueryCreatedLogEventProcessor> logger) :
            base(contractAddressOptions)
        {
            _saltProvider = saltProvider;
            _dataProvider = dataProvider;
            _logger = logger;
            _configOptions = configOptions.Value;
        }

        public override async Task ProcessAsync(LogEvent logEvent)
        {
            var queryCreated = new QueryCreated();
            queryCreated.MergeFrom(logEvent);
            _logger.LogInformation(queryCreated.ToString());

            var nodeAddress = Address.FromBase58(_configOptions.AccountAddress);
            var firstDesignatedNodeAddress = queryCreated.DesignatedNodeList.Value.First();
            var queryToken = queryCreated
                .Token; // Query token means the ethereum contract address oracle node should cares in report case.
            if (queryCreated.DesignatedNodeList.Value.Contains(nodeAddress) ||
                _configOptions.ObserverAssociationAddressList.Contains(firstDesignatedNodeAddress.ToBase58()) ||
                _configOptions.EthereumContractAddress == queryToken ||
                _configOptions.Token == queryToken)
            {
                var data = await _dataProvider.GetDataAsync(queryCreated.QueryId, queryCreated.QueryInfo.UrlToQuery,
                    queryCreated.QueryInfo.AttributesToFetch.ToList());
                if (string.IsNullOrEmpty(data))
                {
                    _logger.LogError($"Failed to response to query {queryCreated.QueryId}.");
                    return;
                }

                var salt = _saltProvider.GetSalt(queryCreated.QueryId);
                _logger.LogInformation($"Queried data: {data}, salt: {salt}");
                var node = new NodeManager(_configOptions.BlockChainEndpoint, _configOptions.AccountAddress,
                    _configOptions.AccountPassword);
                var commitInput = new CommitInput
                {
                    QueryId = queryCreated.QueryId,
                    Commitment = HashHelper.ConcatAndCompute(
                        HashHelper.ComputeFrom(data),
                        HashHelper.ConcatAndCompute(salt, HashHelper.ComputeFrom(_configOptions.AccountAddress)))
                };
                _logger.LogInformation($"Sending Commit tx with input: {commitInput}");
                var txId = node.SendTransaction(_configOptions.AccountAddress, GetContractAddress(), "Commit",
                    commitInput);
                _logger.LogInformation($"[Commit] Tx id {txId}");
            }
        }
    }
}