using System.Threading.Tasks;
using AElf.EventHandler;
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
            var function = web3Manager.GetFunction("0xB09BF7D45a8E7f6917ADf3D97E761B648D9e06C5", "getReceiptInfo");
            var receiptInfo = await function.CallDeserializingToObjectAsync<ReceiptInfo>(0);
            receiptInfo.ShouldBeNull();
        }
    }
}