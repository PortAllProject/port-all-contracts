using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public class ReportConfirmedLogEventProcessor : LogEventProcessorBase, ITransientDependency
    {
        public override string ContractName => "Report";
        public override string LogEventName => nameof(ReportConfirmed);
        private readonly ILogger<QueryCompletedLogEventProcessor> _logger;
        private readonly ISignatureRecoverableInfoProvider _signaturesRecoverableInfoProvider;
        private readonly IReportProvider _reportProvider;
        private readonly EthereumConfigOptions _ethereumConfigOptions;
        private readonly string _abi;

        public ReportConfirmedLogEventProcessor(ILogger<QueryCompletedLogEventProcessor> logger,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions,
            IReportProvider reportProvider,
            ISignatureRecoverableInfoProvider signaturesRecoverableInfoProvider,
            IOptionsSnapshot<EthereumConfigOptions> ethereumConfigOptions) : base(contractAddressOptions)
        {
            _logger = logger;
            _signaturesRecoverableInfoProvider = signaturesRecoverableInfoProvider;
            _reportProvider = reportProvider;
            _ethereumConfigOptions = ethereumConfigOptions.Value;
            var file = ethereumConfigOptions.Value.ContractAbiFilePath;
            if (!string.IsNullOrEmpty(file))
                _abi = ReadJson(file, "abi");
        }

        public override async Task ProcessAsync(LogEvent logEvent)
        {
            var reportConfirmed = new ReportConfirmed();
            reportConfirmed.MergeFrom(logEvent);
            _logger.LogInformation(reportConfirmed.ToString());
            var ethereumContractAddress = reportConfirmed.EthereumContractAddress;
            var roundId = reportConfirmed.RoundId;
            _signaturesRecoverableInfoProvider.SetSignature(ethereumContractAddress, roundId, reportConfirmed.Signature);
            if (reportConfirmed.IsAllNodeConfirm)
            {
                if (_ethereumConfigOptions.IsEnable)
                {
                    var report =
                        _reportProvider.GetReport(ethereumContractAddress, roundId);
                    var signatureRecoverableInfos = _signaturesRecoverableInfoProvider.GetSignature(ethereumContractAddress, roundId);
                    var (reportBytes, rs, ss, vs) = TransferToEthereumParameter(report, signatureRecoverableInfos);
                    var web3Manager = new Web3Manager(_ethereumConfigOptions.Url, _ethereumConfigOptions.Address,
                        _ethereumConfigOptions.PrivateKey,
                        _ethereumConfigOptions.ContractAbiFilePath);
                    await web3Manager.TransmitDataOnEthereum(ethereumContractAddress, reportBytes, rs, ss, vs);
                }
                _signaturesRecoverableInfoProvider.RemoveSignature(ethereumContractAddress, roundId);
                _reportProvider.RemoveReport(ethereumContractAddress, roundId);
            }
        }

        public string ReadJson(string jsonfile, string key)
        {
            using var file = System.IO.File.OpenText(jsonfile);
            using var reader = new JsonTextReader(file);
            var o = (JObject)JToken.ReadFrom(reader);
            var value = o[key]?.ToString();
            return value;
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