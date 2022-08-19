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
    private readonly AElfContractOptions _contractAddressOptions;
    private readonly IReportProvider _reportProvider;
    private readonly IReportService _reportService;
    private readonly IAElfAccountProvider _accountProvider;
    private readonly BridgeOptions _bridgeOptions;
    private readonly AElfClientConfigOptions _aelfClientConfigOptions;

    public override string ContractName => "Report";
    private readonly ILogger<ReportProposedLogEventProcessor> _logger;

    public ReportProposedLogEventProcessor(
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        IReportProvider reportProvider,
        IReportService reportService,
        IAElfAccountProvider accountProvider,
        ILogger<ReportProposedLogEventProcessor> logger,
        IOptionsSnapshot<BridgeOptions> bridgeOptions,
        IOptionsSnapshot<AElfClientConfigOptions> AElfConfigOptions) : base(contractAddressOptions)
    {
        _logger = logger;
        _contractAddressOptions = contractAddressOptions.Value;
        _reportProvider = reportProvider;
        _reportService = reportService;
        _accountProvider = accountProvider;
        _bridgeOptions = bridgeOptions.Value;
        _aelfClientConfigOptions = AElfConfigOptions.Value;
    }

    public override async Task ProcessAsync(LogEvent logEvent)
    {
        var reportProposed = new ReportProposed();
        reportProposed.MergeFrom(logEvent);
        
        _logger.LogInformation($"New report: {reportProposed}");
        
        var privateKey = _accountProvider.GetPrivateKey(_aelfClientConfigOptions.AccountAlias);
        
        var sendTxResult = await _reportService.ConfirmReportAsync(new ConfirmReportInput
        {
            Token = reportProposed.Token,
            RoundId = reportProposed.RoundId,
            Signature = SignHelper
                .GetSignature(reportProposed.RawReport, privateKey).RecoverInfo
        });
        _reportProvider.SetReport(reportProposed.Token, reportProposed.RoundId,
            reportProposed.RawReport);
        _logger.LogInformation($"[ConfirmReport] Tx ： {sendTxResult}");
    }
}