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
        private readonly QueryCreatedLogEventProcessor _queryCreatedLogEventProcessor;

        public LogEventProcessorTests()
        {
            _queryCreatedLogEventProcessor = GetRequiredService<QueryCreatedLogEventProcessor>();
        }

        public async Task QueryCreatedTest()
        {
            await _queryCreatedLogEventProcessor.ProcessAsync(new QueryCreated
            {
                QueryId = HashHelper.ComputeFrom("Test"),
                QueryInfo = new QueryInfo
                {
                    Title = "test",
                    Options = { "foo", "bar" }
                },
                DesignatedNodeList = new AddressList
                {
                    Value = { Address.FromBase58("4zT74bCjganXgwFhcnW8DNLVt3Lebq2speF362oQoAqR4S7WX") }
                }
            }.ToLogEvent());
        }
    }
}