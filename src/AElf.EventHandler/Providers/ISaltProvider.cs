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
        Hash GetSalt(Hash queryId);
    }

    public class SaltProvider : ISaltProvider, ISingletonDependency
    {
        private readonly Dictionary<Hash, Hash> _dictionary;
        private readonly ILogger<SaltProvider> _logger;

        public SaltProvider(ILogger<SaltProvider> logger)
        {
            _logger = logger;
            _dictionary = new Dictionary<Hash, Hash>();
        }

        public Hash GetSalt(Hash queryId)
        {
            // Look up dictionary.
            if (_dictionary.TryGetValue(queryId, out var salt))
            {
                return salt;
            }

            var randomStr = DateTime.UtcNow.Millisecond.ToString(CultureInfo.InvariantCulture) + Guid.NewGuid();
            salt = HashHelper.ConcatAndCompute(queryId, HashHelper.ComputeFrom(randomStr));
            _dictionary[queryId] = salt;
            _logger.LogInformation($"New salt for queryId {queryId}: {salt}. Using random string: {randomStr}");
            return salt;
        }
    }
}