using System.Threading.Tasks;
using AElf.Client.Core;
using AElf.Client.Core.Extensions;
using AElf.Client.Core.Options;
using AElf.Client.Report;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler;

internal class ReportProposedLogEventProcessor : LogEventProcessorBase<ReportProposed>
{
    private readonly IReportProvider _reportProvider;
    private readonly IReportService _reportService;
    private readonly IAElfAccountProvider _accountProvider;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;

    public override string ContractName => "ReportContract";
    private readonly ILogger<ReportProposedLogEventProcessor> _logger;

    public ReportProposedLogEventProcessor(
        IReportProvider reportProvider,
        IReportService reportService,
        IAElfAccountProvider accountProvider,
        ILogger<ReportProposedLogEventProcessor> logger,
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        IOptionsSnapshot<AElfClientConfigOptions> aelfConfigOptions, IChainIdProvider chainIdProvider) : base(
        contractAddressOptions)
    {
        _logger = logger;
        _reportProvider = reportProvider;
        _reportService = reportService;
        _accountProvider = accountProvider;
        _aelfClientConfigOptions = aelfConfigOptions.Value;
    }

    public override async Task ProcessAsync(LogEvent logEvent, EventContext context)
    {
        var reportProposed = new ReportProposed();
        reportProposed.MergeFrom(logEvent);

        _logger.LogInformation($"New report: {reportProposed}");
        
        //TODO:Check permission

        var chainId = ChainIdProvider.GetChainId(context.ChainId);
        var privateKey = _accountProvider.GetPrivateKey(_aelfClientConfigOptions.AccountAlias);
        
        var sendTxResult = await _reportService.ConfirmReportAsync(chainId,new ConfirmReportInput
        {
            ChainId = reportProposed.TargetChainId,
            Token = reportProposed.Token,
            RoundId = reportProposed.RoundId,
            Signature = SignHelper
                .GetSignature(reportProposed.RawReport, privateKey).RecoverInfo
        });
        _logger.LogInformation($"[ConfirmReport] Transaction id ： {sendTxResult.TransactionResult.TransactionId}");
    }
}