using System.Threading.Tasks;
using AElf.Standards.ACS13;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContractTests
    {
        [Fact]
        public async Task ConverterTest()
        {
            var aggregateInput = new AggregateInput
            {
                Results =
                {
                    "1.111", "2.222", "3.333"
                },
                Frequencies = {1, 1, 1}
            };
            var result = await IntegerAggregatorContractStub.Aggregate.CallAsync(aggregateInput);
            result.Value.ShouldBe("2.222");
        }
    }
}