using System.Threading.Tasks;
using AElf.Client.Core;
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

    public override string ContractName => "Report";
    private readonly ILogger<ReportProposedLogEventProcessor> _logger;

    public ReportProposedLogEventProcessor(
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        IReportProvider reportProvider,
        IReportService reportService,
        IAElfAccountProvider accountProvider,
        ILogger<ReportProposedLogEventProcessor> logger) : base(contractAddressOptions)
    {
        _logger = logger;
        _contractAddressOptions = contractAddressOptions.Value;
        _reportProvider = reportProvider;
        _reportService = reportService;
        _accountProvider = accountProvider;
    }

    public override async Task ProcessAsync(LogEvent logEvent)
    {
        // var reportProposed = new ReportProposed();
        // reportProposed.MergeFrom(logEvent);
        //
        // _logger.LogInformation($"New report: {reportProposed}");
        //
        // var sendTxResult = await _reportService.ConfirmReportAsync(new ConfirmReportInput
        // {
        //     Token = _configOptions.TransmitContractAddress,
        //     RoundId = reportProposed.RoundId,
        //     Signature = SignHelper
        //         .GetSignature(reportProposed.RawReport, _keyStore.GetAccountKeyPair().PrivateKey).RecoverInfo
        // });
        // _reportProvider.SetReport(_configOptions.TransmitContractAddress, reportProposed.RoundId,
        //     reportProposed.RawReport);
        // _logger.LogInformation($"[ConfirmReport] Tx id {txId}");
    }
}