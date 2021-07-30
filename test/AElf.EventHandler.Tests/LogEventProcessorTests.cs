using System;
using System.Threading.Tasks;
using AElf.Contracts.Oracle;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Xunit;

namespace AElf.EventHandler.Tests
{
    public sealed class LogEventProcessorTests : AElfEventHandlerTestBase
    {
        private QueryCreatedLogEventProcessor _queryCreatedLogEventProcessor;
        public LogEventProcessorTests()
        {
            _queryCreatedLogEventProcessor = GetRequiredService<QueryCreatedLogEventProcessor>();
        }

        [Fact]
        public async Task QueryCreatedTest()
        {
            await _queryCreatedLogEventProcessor.ProcessAsync(new QueryCreated().ToLogEvent());
            
        }
    }
}