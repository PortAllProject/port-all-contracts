using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public interface IDataProviderSelector
    {
        Type Select(string queryTitle);
    }

    public class DataProviderSelector : IDataProviderSelector, ITransientDependency
    {
        private DataProviderOptions Options { get; }

        public DataProviderSelector(IOptions<DataProviderOptions> options)
        {
            Options = options.Value;
        }

        public Type Select(string queryTitle)
        {
            return Options.DataProviders.GetOrDefault(queryTitle)
                   ?? typeof(UrlDataProvider);
        }
    }
}