using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public interface ISignatureRecoverableInfoProvider
    {
        void SetSignature(string ethereumContractAddress, long roundId, string recoverableInfo);
        HashSet<string> GetSignature(string ethereumContractAddress, long roundId);
        void RemoveSignature(string ethereumContractAddress, long roundId);
    }

    public class SignatureRecoverableInfoProvider : ISignatureRecoverableInfoProvider, ISingletonDependency
    {
        private readonly Dictionary<string, Dictionary<long, HashSet<string>>> _signatureRecoverableInfoDictionary;
        private ILogger<SignatureRecoverableInfoProvider> _logger;

        public SignatureRecoverableInfoProvider(ILogger<SignatureRecoverableInfoProvider> logger)
        {
            _signatureRecoverableInfoDictionary = new Dictionary<string, Dictionary<long, HashSet<string>>>();
            _logger = logger;
        }

        public void SetSignature(string ethereumContractAddress, long roundId, string recoverableInfo)
        {
            if (!_signatureRecoverableInfoDictionary.TryGetValue(ethereumContractAddress, out var roundSignature))
            {
                roundSignature = new Dictionary<long, HashSet<string>>();
                _signatureRecoverableInfoDictionary[ethereumContractAddress] = roundSignature;
            }
            if (!roundSignature.ContainsKey(roundId))
                roundSignature[roundId] = new HashSet<string>();
            roundSignature[roundId].Add(recoverableInfo);
        }

        public HashSet<string> GetSignature(string ethereumContractAddress, long roundId)
        {
            if (!_signatureRecoverableInfoDictionary.TryGetValue(ethereumContractAddress, out var roundSignature))
            {
                _logger.LogInformation($"Address: {ethereumContractAddress} report dose not exist");
                return new HashSet<string>();
            }

            if (roundSignature.TryGetValue(roundId, out var recoverableInfo)) return recoverableInfo;
            _logger.LogInformation($"Address: {ethereumContractAddress} RoundId: {roundId} report dose not exist");
                return new HashSet<string>();
        }
        
        public void RemoveSignature(string ethereumContractAddress, long roundId)
        {
            if (!_signatureRecoverableInfoDictionary.TryGetValue(ethereumContractAddress, out var roundSignature))
                return;
            if (!roundSignature.TryGetValue(roundId, out _))
                return;
            roundSignature.Remove(roundId);
            if (roundSignature.Count == 0)
                _signatureRecoverableInfoDictionary.Remove(ethereumContractAddress);
        }
    }
}