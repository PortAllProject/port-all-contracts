using System.Globalization;
using System.Threading.Tasks;
using AElf.Standards.ACS13;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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
                    new StringValue {Value = 1.111.ToString(CultureInfo.InvariantCulture)}.ToByteString(),
                    new StringValue {Value = 2.222.ToString(CultureInfo.InvariantCulture)}.ToByteString(),
                    new StringValue {Value = 3.333.ToString(CultureInfo.InvariantCulture)}.ToByteString(),
                },
                Frequencies = {1, 1, 1}
            };
            var result = await IntegerAggregatorContractStub.Aggregate.CallAsync(aggregateInput);
            result.Value.ShouldBe(new StringValue {Value = "2.222"}.ToByteString());
        }
    }
}