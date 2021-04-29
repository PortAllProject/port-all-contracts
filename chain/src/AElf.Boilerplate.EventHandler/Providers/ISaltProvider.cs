using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Boilerplate.EventHandler
{
    public interface ISaltProvider
    {
        Hash GetSalt(Hash queryId);
    }

    public class SaltProvider : ISaltProvider, ISingletonDependency
    {
        private readonly Dictionary<Hash, Hash> _dictionary;

        public SaltProvider()
        {
            _dictionary = new Dictionary<Hash, Hash>();
        }

        public Hash GetSalt(Hash queryId)
        {
            // Look up dictionary.
            if (_dictionary.TryGetValue(queryId, out var salt))
            {
                return salt;
            }

            var randomStr = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture).Concat(Guid.NewGuid().ToString())
                .ToString();
            salt = HashHelper.ConcatAndCompute(queryId, HashHelper.ComputeFrom(randomStr));
            _dictionary[queryId] = salt;
            return salt;
        }
    }
}