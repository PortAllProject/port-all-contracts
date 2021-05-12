
using System;
using System.Threading.Tasks;
using Nethereum.Web3;

namespace AElf.Boilerplate.EventHandler
{
    public class Web3Manager
    {
        private const string MethodName = "transmit";
        private readonly string _senderAddress;
        private readonly string _abiCode;
        private readonly Web3 _web3;
        
        public Web3Manager(string url, string senderAddress, string privateKey, string abiCode)
        {
            _senderAddress = senderAddress;
            _abiCode = abiCode;
            var account = new Nethereum.Web3.Accounts.Account(privateKey);
            _web3 = new Web3(account, url);
        }

        public async Task TransmitDataOnEthereum(string contractAddress, byte[] report, byte[][] rs, byte[][] ss, byte[] rawVs)
        {
            var contract = _web3.Eth.GetContract(_abiCode, contractAddress);
            
            var setValueFunction = contract.GetFunction(MethodName);
            try
            {
                var gas = await setValueFunction.EstimateGasAsync(_senderAddress, null, null, report, rs, ss, rawVs);
                await setValueFunction.SendTransactionAsync(_senderAddress, gas, null, null, report, rs, ss, rawVs);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            
        }
    }
}