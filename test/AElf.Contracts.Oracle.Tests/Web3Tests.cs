using AElf.Contracts.Bridge;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Oracle
{
    public class Web3Tests
    {
        [Fact]
        protected void Test()
        {
            var stringValue = new StringValue
            {
                Value = " { \"value\": { \"0\": \"9284ba19f300b9fa9f4afba12f1d786a18d077db95063ad44233aa68dd47031f\", \"1\": \"4dadc626d2c2dadb02f8a6ccd4474dcca47bc202987339a96d8fdf61793d496b\" } }"
            };
            var map = JsonParser.Default.Parse<ReceiptHashMap>(stringValue.Value);
            map.ShouldBeNull();
        }
    }
}