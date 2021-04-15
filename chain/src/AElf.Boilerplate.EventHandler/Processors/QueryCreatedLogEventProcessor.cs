using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.Contracts.Oracle;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public class QueryCreatedLogEventProcessor : ILogEventProcessor, ITransientDependency
    {
        private readonly ISaltProvider _saltProvider;
        private readonly IDataProvider _dataProvider;
        private readonly ContractAddressOptions _contractAddressOptions;
        private readonly ConfigOptions _configOptions;
        public string ContractName => "Oracle";
        public string LogEventName => nameof(QueryCreated);

        public QueryCreatedLogEventProcessor(IOptionsSnapshot<ConfigOptions> configOptions,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions,
            ISaltProvider saltProvider, IDataProvider dataProvider)
        {
            _saltProvider = saltProvider;
            _dataProvider = dataProvider;
            _contractAddressOptions = contractAddressOptions.Value;
            _configOptions = configOptions.Value;
        }

        public async Task ProcessAsync(LogEvent logEvent)
        {
            var queryCreated = new QueryCreated();
            queryCreated.MergeFrom(logEvent);
            if (queryCreated.Token != _configOptions.EthereumContractAddress &&
                !queryCreated.DesignatedNodeList.Value.Contains(Address.FromBase58(_configOptions.AccountAddress)) &&
                queryCreated.DesignatedNodeList.Value.First().ToBase58() != _configOptions.ObserverAssociationAddress)
                return;

            var node = new NodeManager(_configOptions.BlockChainEndpoint);
            node.SendTransaction(_configOptions.AccountAddress,
                _contractAddressOptions.ContractAddressMap[ContractName], "Commit", new CommitInput
                {
                    QueryId = queryCreated.QueryId,
                    Commitment = HashHelper.ConcatAndCompute(
                        HashHelper.ComputeFrom(new StringValue
                        {
                            Value = await _dataProvider.GetDataAsync(queryCreated.QueryId, queryCreated.UrlToQuery,
                                queryCreated.AttributeToFetch)
                        }),
                        _saltProvider.GetSalt(queryCreated.QueryId))
                });
        }
    }
}