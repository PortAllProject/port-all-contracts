using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.Core.Extensions;
using AElf.Client.Core.Options;
using AElf.Client.Report;
using AElf.Contracts.Report;
using AElf.Nethereum.Core.Options;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler;

internal class ReportConfirmedLogEventProcessor : LogEventProcessorBase<ReportConfirmed>
{
    public override string ContractName => "ReportContract";
    private readonly ILogger<ReportConfirmedLogEventProcessor> _logger;
    private readonly ISignatureRecoverableInfoProvider _signaturesRecoverableInfoProvider;
    private readonly EthereumContractOptions _ethereumContractOptions;
    private readonly IReportProvider _reportProvider;
    private readonly string _abi;
    private readonly ITransmitTransactionProvider _transmitTransactionProvider;
    private readonly BridgeOptions _bridgeOptions;

    public ReportConfirmedLogEventProcessor(ILogger<ReportConfirmedLogEventProcessor> logger,
        IOptionsSnapshot<AElfContractOptions> contractAddressOptions,
        IReportProvider reportProvider,
        ISignatureRecoverableInfoProvider signaturesRecoverableInfoProvider,
        IOptionsSnapshot<EthereumContractOptions> ethereumContractOptions,
        ITransmitTransactionProvider transmitTransactionProvider,
        IOptionsSnapshot<BridgeOptions> bridgeOptions,
        IReportService reportContractService) : base(contractAddressOptions)
    {
        _logger = logger;
        _signaturesRecoverableInfoProvider = signaturesRecoverableInfoProvider;
        _transmitTransactionProvider = transmitTransactionProvider;
        _ethereumContractOptions = ethereumContractOptions.Value;
        _reportProvider = reportProvider;
        _bridgeOptions = bridgeOptions.Value;
        var file = Path.Combine(_ethereumContractOptions.AbiFileDirectory,
            _ethereumContractOptions.ContractInfoList["Bridge"].AbiFileName);
        if (!string.IsNullOrEmpty(file))
            _abi = JsonHelper.ReadJson(file, "abi");
    }

    public override async Task ProcessAsync(LogEvent logEvent, EventContext context)
    {
        var reportConfirmed = new ReportConfirmed();
        reportConfirmed.MergeFrom(logEvent);
        _logger.LogInformation(reportConfirmed.ToString());
        var ethereumContractAddress = reportConfirmed.Token;
        var roundId = reportConfirmed.RoundId;
        await _signaturesRecoverableInfoProvider.SetSignatureAsync(context.ChainId.ToString(),ethereumContractAddress, roundId,
            reportConfirmed.Signature);
        if (reportConfirmed.IsAllNodeConfirmed)
        {
            if (_bridgeOptions.IsTransmitter)
            {
                var report =
                    _reportProvider.GetReport(ethereumContractAddress, roundId);
                var signatureRecoverableInfos =
                    await _signaturesRecoverableInfoProvider.GetSignatureAsync(context.ChainId.ToString(),
                        ethereumContractAddress, roundId);
                var (reportBytes, rs, ss, vs) = TransferToEthereumParameter(report, signatureRecoverableInfos);

                _logger.LogInformation(
                    $"Try to transmit data, TargetChainId: {reportConfirmed.TargetChainId} Address: {ethereumContractAddress}  RoundId: {reportConfirmed.RoundId}");
                
                await _transmitTransactionProvider.EnqueueAsync(new SendTransmitArgs
                {
                    ChainId = context.ChainId.ToString(),
                    TargetContractAddress = ethereumContractAddress,
                    TargetChainId = reportConfirmed.TargetChainId,
                    Report = reportBytes,
                    Rs = rs,
                    Ss = ss,
                    RawVs = vs,
                });

                await _signaturesRecoverableInfoProvider.RemoveSignatureAsync(context.ChainId.ToString(),
                    ethereumContractAddress, roundId);
                _reportProvider.RemoveReport(ethereumContractAddress, roundId);
            }
        }
    }

    public (byte[], byte[][], byte[][], byte[]) TransferToEthereumParameter(string report,
        HashSet<string> recoverableInfos)
    {
        var signaturesCount = recoverableInfos.Count;
        var r = new byte[signaturesCount][];
        var s = new byte[signaturesCount][];
        var v = new byte[32];
        var index = 0;
        foreach (var recoverableInfoBytes in recoverableInfos.Select(recoverableInfo =>
                     ByteStringHelper.FromHexString(recoverableInfo).ToByteArray()))
        {
            r[index] = recoverableInfoBytes.Take(32).ToArray();
            s[index] = recoverableInfoBytes.Skip(32).Take(32).ToArray();
            v[index] = recoverableInfoBytes.Last();
            index++;
        }

        return (ByteStringHelper.FromHexString(report).ToByteArray(), r, s, v);
    }
}