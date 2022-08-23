using System.Threading.Tasks;
using AElf.Client.Core.Extensions;
using AElf.Client.Core.Options;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Types;
using Microsoft.Extensions.Options;

namespace AElf.EventHandler;

public class IrreversibleBlockFoundLogEventProcessor : LogEventProcessorBase<IrreversibleBlockFound>
{
    private readonly ITransmitTransactionProvider _transmitTransactionProvider;

    public IrreversibleBlockFoundLogEventProcessor(
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        ITransmitTransactionProvider transmitTransactionProvider) : base(contractAddressOptions)
    {
        _transmitTransactionProvider = transmitTransactionProvider;
    }

    public override string ContractName => "ConsensusContract";

    public override async Task ProcessAsync(LogEvent logEvent, EventContext context)
    {
        var libFound = new IrreversibleBlockFound();
        libFound.MergeFrom(logEvent);
        // _logger.LogInformation($"IrreversibleBlockFound: {libFound}");
        //
        //
        // await _transmitTransactionProvider.SendByLibAsync()
    }
}