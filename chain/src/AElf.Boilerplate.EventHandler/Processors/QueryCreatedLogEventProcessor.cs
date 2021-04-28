using System;
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
    public class QueryCreatedLogEventProcessor : ILogEventProcessor, ISingletonDependency
    {
        private readonly ISaltProvider _saltProvider;
        private readonly IDataProvider _dataProvider;
        private readonly ContractAddressOptions _contractAddressOptions;
        private readonly ConfigOptions _configOptions;
        public string ContractName => "Oracle";
        public string LogEventName => nameof(QueryCreated);
        private readonly ILogger<QueryCreatedLogEventProcessor> _logger;

        public QueryCreatedLogEventProcessor(IOptionsSnapshot<ConfigOptions> configOptions,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions,
            ISaltProvider saltProvider, IDataProvider dataProvider, ILogger<QueryCreatedLogEventProcessor> logger)
        {
            _saltProvider = saltProvider;
            _dataProvider = dataProvider;
            _logger = logger;
            _contractAddressOptions = contractAddressOptions.Value;
            _configOptions = configOptions.Value;
        }

        public async Task ProcessAsync(LogEvent logEvent)
        {
            var queryCreated = new QueryCreated();
            queryCreated.MergeFrom(logEvent);
            _logger.LogInformation(queryCreated.ToString());
            if (queryCreated.Token != _configOptions.EthereumContractAddress &&
                !queryCreated.DesignatedNodeList.Value.Contains(Address.FromBase58(_configOptions.AccountAddress)) &&
                !_configOptions.ObserverAssociationAddressList.Contains(queryCreated.DesignatedNodeList.Value.First()
                    .ToBase58()))
                return;

            var node = new NodeManager(_configOptions.BlockChainEndpoint, _configOptions.AccountAddress,
                _configOptions.AccountPassword);
            var commitInput = new CommitInput
            {
                QueryId = queryCreated.QueryId,
                Commitment = HashHelper.ConcatAndCompute(
                    HashHelper.ComputeFrom(new StringValue
                    {
                        Value = await _dataProvider.GetDataAsync(queryCreated.QueryId, queryCreated.UrlToQuery,
                            queryCreated.AttributeToFetch)
                    }),
                    _saltProvider.GetSalt(queryCreated.QueryId))
            };
            _logger.LogInformation($"Sent Commit tx with input: {commitInput}");
            node.SendTransaction(_configOptions.AccountAddress,
                _contractAddressOptions.ContractAddressMap[ContractName], "Commit", commitInput);
        }
    }
}