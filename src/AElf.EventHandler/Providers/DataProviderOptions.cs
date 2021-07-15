using System;
using System.Collections.Generic;

namespace AElf.EventHandler
{
    public class DataProviderOptions
    {
        public Dictionary<string, Type> DataProviders { get; }

        public DataProviderOptions()
        {
            DataProviders = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        }
    }
}