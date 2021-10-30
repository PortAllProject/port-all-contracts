using System.Threading.Tasks;
using AElf.EventHandler;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Bridge.Tests
{
    public class Web3Test
    {
        [Fact]
        public async Task BSCTest()
        {
            const string url = "https://speedy-nodes-nyc.moralis.io/00445b55c0169de2fdc5ef07/bsc/testnet";
            const string senderAddress = "0xB240915a9D4505503a28B8c23f6ea4aAcE4d34E7";
            const string privateKey = "0x2abfd8a6bd92d4e17a8fb2621a07fd723487e8690f2a575d5b321be1481ceae2";
            const string contractAddress = "0x0F3757DA3Ef5A71a67BEe2D3e97e62d0aaf3Da26";
            var abiCode = string.Empty;
            {
                var file = "../../../../../src/AElf.EventHandler/ContractBuild/LockWithTakeTokenAbi.json";
                abiCode = JsonHelper.ReadJson(file, "abi");
            }
            var web3Manager = new Web3Manager(url, senderAddress, privateKey, abiCode);
            var function = web3Manager.GetFunction(contractAddress, "getReceiptInfo");
            var receiptInfo = await function.CallDeserializingToObjectAsync<EventHandler.ReceiptInfo>(0);
            receiptInfo.TargetAddress.ShouldNotBeNull();
        }
    }
}