using System;
using System.Collections.Generic;
using System.Globalization;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public interface ISaltProvider
    {
        Hash GetSalt(string chainId, Hash queryId);
    }

    public class SaltProvider : ISaltProvider, ISingletonDependency
    {
        private readonly Dictionary<string, Hash> _dictionary;
        private readonly ILogger<SaltProvider> _logger;

        public SaltProvider(ILogger<SaltProvider> logger)
        {
            _logger = logger;
            _dictionary = new Dictionary<string, Hash>();
        }

        public Hash GetSalt(string chainId, Hash queryId)
        {
            // Look up dictionary.
            var key = chainId + queryId.ToHex();
            if (_dictionary.TryGetValue(key, out var salt))
            {
                return salt;
            }

            var randomStr = DateTime.UtcNow.Millisecond.ToString(CultureInfo.InvariantCulture) + Guid.NewGuid();
            salt = HashHelper.ConcatAndCompute(queryId, HashHelper.ComputeFrom(randomStr));
            _dictionary[key] = salt;
            _logger.LogInformation($"New salt for queryId {queryId}: {salt}. Using random string: {randomStr}");
            return salt;
        }
    }
}