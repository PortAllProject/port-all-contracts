using System.Threading.Tasks;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace AElf.TokenSwap
{
    public class Web3Manager
    {
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

        public Function GetFunction(string contractAddress, string methodName)
        {
            var contract = _web3.Eth.GetContract(_abiCode, contractAddress);
            return contract.GetFunction(methodName);
        }

        public async Task<TransactionReceipt> GetTransactionReceipt(string transactionHash)
        {
            return await _web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
        }
    }
}