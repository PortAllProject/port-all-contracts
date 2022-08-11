using System.Threading.Tasks;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;

namespace AElf.EventHandler;

public class Web3Manager
{
    private const string MethodName = "transmit";
    private readonly string _senderAddress;
    private readonly string _abiCode;
    private readonly Web3 _web3;
    public string BaseUrl { get; set; }

    public Web3Manager(string url, string senderAddress, string privateKey, string abiCode)
    {
        _senderAddress = senderAddress;
        _abiCode = abiCode;
        var account = new Account(privateKey);
        _web3 = new Web3(account, url);
        BaseUrl = url;
    }

    public async Task TransmitDataOnEthereum(string contractAddress, byte[] report, byte[][] rs, byte[][] ss,
        byte[] rawVs)
    {
        var contract = _web3.Eth.GetContract(_abiCode, contractAddress);
        var setValueFunction = contract.GetFunction(MethodName);
        var gas = await setValueFunction.EstimateGasAsync(_senderAddress, null, null, report, rs, ss, rawVs);
        await setValueFunction.SendTransactionAsync(_senderAddress, gas, null, null, report, rs, ss, rawVs);
    }

    public async Task<TransactionReceipt> TransmitDataOnEthereumWithReceipt(string contractAddress, byte[] report,
        byte[][] rs, byte[][] ss, byte[] rawVs)
    {
        var contract = _web3.Eth.GetContract(_abiCode, contractAddress);
        var setValueFunction = contract.GetFunction(MethodName);
        var gas = await setValueFunction.EstimateGasAsync(_senderAddress, null, null, report, rs, ss, rawVs);
        var transactionResult =
            await setValueFunction.SendTransactionAndWaitForReceiptAsync(_senderAddress, gas, null, null, report,
                rs, ss, rawVs);
        return transactionResult;
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