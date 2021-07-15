using System.Threading.Tasks;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace AElf.EventHandler
{
    public interface INethereumManager
    {
        Web3 Web3 { get; }

        Contract Contract { get; }

        Function GetFunction(string name);

        Task<TransactionReceipt> SendTransactionAndWaitForReceiptAsync(string functionName,
            params object[] parameters);
    }

    public class NethereumManager : INethereumManager
    {
        private readonly string _contractAddress;
        private readonly string _contractAbi;
        private readonly string _senderAddress;

        public Web3 Web3 { get; }
        public Contract Contract => Web3.Eth.GetContract(_contractAbi, _contractAddress);

        public NethereumManager(string contractAddress, string contractAbi, string senderAddress, string privateKey,
            string endpointUrl)
        {
            _contractAddress = contractAddress;
            _contractAbi = contractAbi;
            _senderAddress = senderAddress;
            var account = new Nethereum.Web3.Accounts.Account(privateKey);
            Web3 = new Web3(account, endpointUrl);
        }

        public Function GetFunction(string name)
        {
            return Contract.GetFunction(name);
        }

        public async Task<TransactionReceipt> SendTransactionAndWaitForReceiptAsync(string functionName,
            params object[] parameters)
        {
            var function = GetFunction(functionName);
            var gas = await function.EstimateGasAsync(_senderAddress, null, null, parameters);
            var transactionReceipt =
                await function.SendTransactionAndWaitForReceiptAsync(_senderAddress, gas, null, null, parameters);
            return transactionReceipt;
        }
    }
}