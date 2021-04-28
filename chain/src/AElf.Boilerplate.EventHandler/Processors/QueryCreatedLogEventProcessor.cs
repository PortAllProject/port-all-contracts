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
            if (queryCreated.Token != _configOptions.EthereumContractAddress &&
                !queryCreated.DesignatedNodeList.Value.Contains(Address.FromBase58(_configOptions.AccountAddress)) &&
                !_configOptions.ObserverAssociationAddressList.Contains(queryCreated.DesignatedNodeList.Value.First()
                    .ToBase58()))
                return;

            var data = await _dataProvider.GetDataAsync(queryCreated.QueryId, queryCreated.QueryInfo.UrlToQuery,
                queryCreated.QueryInfo.AttributesToFetch.ToList());
            _logger.LogInformation($"Queried data: {data}");
            var node = new NodeManager(_configOptions.BlockChainEndpoint, _configOptions.AccountAddress,
                _configOptions.AccountPassword);
            var commitInput = new CommitInput
            {
                QueryId = queryCreated.QueryId,
                Commitment = HashHelper.ConcatAndCompute(
                    HashHelper.ComputeFrom(new StringValue
                    {
                        Value = data
                    }),
                    _saltProvider.GetSalt(queryCreated.QueryId))
            };
            _logger.LogInformation($"Sending Commit tx with input: {commitInput}");
            var txId = node.SendTransaction(_configOptions.AccountAddress, GetContractAddress(), "Commit", commitInput);
            _logger.LogInformation($"Tx id: {txId}");
        }
    }
}