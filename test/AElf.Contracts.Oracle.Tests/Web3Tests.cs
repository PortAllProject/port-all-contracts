using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Bridge;
using AElf.EventHandler;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Oracle
{
    public class Web3Tests
    {
        [Fact]
        public async Task MultiResultTest()
        {
            var file = "/Users/eanzhao/Code/port-all-contracts/src/AElf.EventHandler/ContractBuild/LockAbi.json";
            var abi = JsonHelper.ReadJson(file, "abi");
            var web3Manager = new Web3Manager("https://kovan.infura.io/v3/f4b32151507d420e9bd411a41aef00ff",
                "0xB240915a9D4505503a28B8c23f6ea4aAcE4d34E7",
                "0x2abfd8a6bd92d4e17a8fb2621a07fd723487e8690f2a575d5b321be1481ceae2", abi);
            var receiptInfoList = new List<ReceiptInfo>();

            var receiptInfoFunction =
                web3Manager.GetFunction("0xB09BF7D45a8E7f6917ADf3D97E761B648D9e06C5", "getReceiptInfo");
            for (var i = 0; i <= 12; i++)
            {
                var receiptInfo = await receiptInfoFunction.CallDeserializingToObjectAsync<ReceiptInfo>(i);
                receiptInfoList.Add(receiptInfo);
            }

            var str = receiptInfoList.Aggregate(string.Empty, (current, receiptInfo) => current + '\n' + receiptInfo);

            str.ShouldBeNull();
        }
        
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