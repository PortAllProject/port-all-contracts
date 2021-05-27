using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public interface IReportProvider
    {
        void SetReport(string ethereumContractAddress, long roundId, string report);
        string GetReport(string ethereumContractAddress, long roundId);
        void RemoveReport(string ethereumContractAddress, long roundId);

    }
    public class ReportProvider : IReportProvider, ISingletonDependency
    {
        private readonly Dictionary<string, Dictionary<long, string>> _reportDictionary;
        private ILogger<ReportProvider> _logger;
        public ReportProvider(ILogger<ReportProvider> logger)
        {
            _reportDictionary = new Dictionary<string, Dictionary<long, string>>();
            _logger = logger;
        }

        public void SetReport(string ethereumContractAddress, long roundId, string report)
        {
            if (!_reportDictionary.TryGetValue(ethereumContractAddress, out var roundReport))
            {
                roundReport = new Dictionary<long, string>();
                _reportDictionary[ethereumContractAddress] = roundReport;
            }
            if (!roundReport.ContainsKey(roundId))
                roundReport[roundId] = report;
        }

        public string GetReport(string ethereumContractAddress, long roundId)
        {
            if (!_reportDictionary.TryGetValue(ethereumContractAddress, out var roundReport))
            {
                _logger.LogInformation($"Address: {ethereumContractAddress} report dose not exist");
                return string.Empty;
            }

            if (roundReport.TryGetValue(roundId, out var report)) return report;
            _logger.LogInformation($"Address: {ethereumContractAddress} RoundId: {roundId} report dose not exist");
            return string.Empty;

        }

        public void RemoveReport(string ethereumContractAddress, long roundId)
        {
            if (!_reportDictionary.TryGetValue(ethereumContractAddress, out var roundReport))
                return;
            if (!roundReport.TryGetValue(roundId, out _))
                return;
            roundReport.Remove(roundId);
            if (roundReport.Count == 0)
                _reportDictionary.Remove(ethereumContractAddress);
        }
    }
}