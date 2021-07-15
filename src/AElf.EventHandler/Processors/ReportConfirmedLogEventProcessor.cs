using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.EventHandler
{
    internal class ReportConfirmedLogEventProcessor : LogEventProcessorBase<ReportConfirmed>
    {
        public override string ContractName => "Report";
        private readonly ILogger<ReportConfirmedLogEventProcessor> _logger;
        private readonly ISignatureRecoverableInfoProvider _signaturesRecoverableInfoProvider;
        private readonly INethereumManagerFactory _nethereumManagerFactory;
        private readonly IReportProvider _reportProvider;
        private readonly EthereumConfigOptions _ethereumConfigOptions;

        public ReportConfirmedLogEventProcessor(ILogger<ReportConfirmedLogEventProcessor> logger,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions,
            IReportProvider reportProvider,
            ISignatureRecoverableInfoProvider signaturesRecoverableInfoProvider,
            IOptionsSnapshot<EthereumConfigOptions> ethereumConfigOptions,
            INethereumManagerFactory nethereumManagerFactory) : base(contractAddressOptions)
        {
            _logger = logger;
            _signaturesRecoverableInfoProvider = signaturesRecoverableInfoProvider;
            _nethereumManagerFactory = nethereumManagerFactory;
            _reportProvider = reportProvider;
            _ethereumConfigOptions = ethereumConfigOptions.Value;
        }

        public override async Task ProcessAsync(LogEvent logEvent)
        {
            var reportConfirmed = new ReportConfirmed();
            reportConfirmed.MergeFrom(logEvent);
            _logger.LogInformation(reportConfirmed.ToString());
            var ethereumContractAddress = reportConfirmed.Token;
            var roundId = reportConfirmed.RoundId;
            _signaturesRecoverableInfoProvider.SetSignature(ethereumContractAddress, roundId,
                reportConfirmed.Signature);
            if (reportConfirmed.IsAllNodeConfirmed)
            {
                if (_ethereumConfigOptions.IsEnable)
                {
                    var report =
                        _reportProvider.GetReport(ethereumContractAddress, roundId);
                    var signatureRecoverableInfos =
                        _signaturesRecoverableInfoProvider.GetSignature(ethereumContractAddress, roundId);
                    var (reportBytes, rs, ss, vs) = TransferToEthereumParameter(report, signatureRecoverableInfos);
                    try
                    {
                        _logger.LogInformation(
                            $"Try to transmit data to Ethereum, Address: {ethereumContractAddress}  RoundId: {reportConfirmed.RoundId}");

                        var nethereumManager = _nethereumManagerFactory.CreateManager(new TransmitContractNameProvider());
                        var transactionReceipt =
                            await nethereumManager.SendTransactionAndWaitForReceiptAsync("transmit", reportBytes,
                                rs, ss, vs);
                        if (transactionReceipt.HasErrors().Value)
                        {
                            _logger.LogInformation("Failed to transmit data.");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogInformation("Failed to transmit data:");
                        _logger.LogInformation(ex.Message);
                    }
                }

                _signaturesRecoverableInfoProvider.RemoveSignature(ethereumContractAddress, roundId);
                _reportProvider.RemoveReport(ethereumContractAddress, roundId);
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
}