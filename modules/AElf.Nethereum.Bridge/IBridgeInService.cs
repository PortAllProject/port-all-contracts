using AElf.Nethereum.Core;
using Nethereum.RPC.Eth.DTOs;
using Volo.Abp.DependencyInjection;

namespace AElf.Nethereum.Bridge;

public interface IBridgeInService
{
    Task<TransactionReceipt> TransmitAsync(string clientAlias, byte[] report, byte[][] rs, byte[][] ss, byte[] rawVs);
}

public class BridgeInService : ContractServiceBase,IBridgeInService,ITransientDependency
{
    protected override string SmartContractName { get; } = "BridgeIn";

    public async Task<TransactionReceipt> TransmitAsync(string clientAlias, byte[] report, byte[][] rs, byte[][] ss, byte[] rawVs)
    {
        var setValueFunction = GetFunction(clientAlias, "transmit");
        var sender = GetAccount(clientAlias).Address;
        var gas = await setValueFunction.EstimateGasAsync(sender, null, null, report, rs, ss, rawVs);
        var transactionResult =
            await setValueFunction.SendTransactionAndWaitForReceiptAsync(sender, gas, null, null, report,
                rs, ss, rawVs);
        return transactionResult;
    }

    
}