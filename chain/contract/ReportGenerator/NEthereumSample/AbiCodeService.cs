using System;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ReportGenerator
{
    public class AbiCodeService
    {
        private string _url;
        private string _privateKey;
        private string _address;
        private Web3 _web3;
        public AbiCodeService()
        {
            //"https://kovan.infura.io/v3/"
            _url = "https://kovan.infura.io/v3/4c41b8476e874c178c633ff442a27a1b"; //api key
            _privateKey = "0x2abfd8a6bd92d4e17a8fb2621a07fd723487e8690f2a575d5b321be1481ceae2";
            _address = "0xB240915a9D4505503a28B8c23f6ea4aAcE4d34E7";
            var account = new Account(_privateKey);
            _web3 = new Web3(account, _url);
            //_web3 = new Web3(_url);
        }

        public async Task SetValue(string contractAddress, string abi, int newValue)
        {
            var contract = _web3.Eth.GetContract(abi, contractAddress);
            const string methodName = "setValue";
            var setValueFunction = contract.GetFunction(methodName);
            var bigNewValue = new BigInteger(newValue);
            try
            {
                var gas = await setValueFunction.EstimateGasAsync(_address, null, null, bigNewValue);
                await setValueFunction.SendTransactionAsync(_address, gas, null, null, bigNewValue);
                // var receiptAmountSend =
                //     await setValueFunction.SendTransactionAndWaitForReceiptAsync(_address, gas, null, null, bigNewValue);
                // await setValueFunction.SendTransactionAsync(new TransactionInput
                // {
                // });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);
            }
            
        }

        public async Task<long> GetValue(string contractAddress, string abi)
        {
            var contract = _web3.Eth.GetContract(abi, contractAddress);
            const string methodName = "getValue";
            var getValueFunction = contract.GetFunction(methodName);
            var getValue = await getValueFunction.CallAsync<long>();
            return getValue;
        }
        
        public async Task TransmitValue(string contractAddress, string abi, byte[] report, byte[][] rs, byte[][] ss, byte[] rawVs)
        {
            var contract = _web3.Eth.GetContract(abi, contractAddress);
            const string methodName = "transmit";
            var setValueFunction = contract.GetFunction(methodName);
            try
            {
                var gas = await setValueFunction.EstimateGasAsync(_address, null, null, report, rs, ss, rawVs);
                await setValueFunction.SendTransactionAsync(_address, gas, null, null, report, rs, ss, rawVs);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Message);
            }
        }
        
        public static string ReadJson(string jsonfile, string key)
        {
            using var file = System.IO.File.OpenText(jsonfile);
            using var reader = new JsonTextReader(file);
            var o = (JObject)JToken.ReadFrom(reader);
            var value = o[key].ToString();
            return value;
        }
    }
}