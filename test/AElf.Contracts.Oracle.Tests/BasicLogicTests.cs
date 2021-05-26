using System.Text;
using System.Threading.Tasks;
using AElf.Standards.ACS13;
using Google.Protobuf;
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

        [Fact]
        public void TypeTest()
        {
            var byteString = ByteString.CopyFrom("eantest".GetBytes());
            byteString.Length.ShouldBe(7);
            byteString.ToByteArray().Length.ShouldBe(7);
            Encoding.UTF8.GetString(byteString.ToByteArray()).ShouldBe("eantest");

            const string test = "testtfdsafdsafdsafsa";
            var bytes = test.GetBytes();
            byteString = ByteString.CopyFrom(bytes);
            byteString.ToByteArray().ShouldBe(bytes);
        }
    }
}