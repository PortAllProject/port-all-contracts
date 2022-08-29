using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Extensions;
using AElf.Client.Core.Options;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.EventHandler;

public class IrreversibleBlockFoundLogEventProcessor : LogEventProcessorBase<IrreversibleBlockFound>
{
    private readonly ITransmitTransactionProvider _transmitTransactionProvider;
    private readonly IAElfClientService _aelfClientService;
    private readonly AElfChainAliasOptions _chainAliasOptions;
    public ILogger<IrreversibleBlockFoundLogEventProcessor> Logger { get; set; }

    public IrreversibleBlockFoundLogEventProcessor(
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        ITransmitTransactionProvider transmitTransactionProvider, IAElfClientService aelfClientService,
        IOptionsSnapshot<AElfChainAliasOptions> chainAliasOptions) : base(
        contractAddressOptions)
    {
        _transmitTransactionProvider = transmitTransactionProvider;
        _aelfClientService = aelfClientService;
        _chainAliasOptions = chainAliasOptions.Value;

        Logger = NullLogger<IrreversibleBlockFoundLogEventProcessor>.Instance;
    }

    public override string ContractName => "ConsensusContract";

    public override async Task ProcessAsync(LogEvent logEvent, EventContext context)
    {
        var libFound = new IrreversibleBlockFound();
        libFound.MergeFrom(logEvent);
        //Logger.LogInformation($"IrreversibleBlockFound: {libFound}");

        var chainId = ChainIdProvider.GetChainId(context.ChainId);
        var clientAlias = _chainAliasOptions.Mapping[chainId];
        var block = await _aelfClientService.GetBlockByHeightAsync(clientAlias,libFound.IrreversibleBlockHeight);
        await _transmitTransactionProvider.SendByLibAsync(chainId, block.BlockHash, block.Header.Height);
    }
}