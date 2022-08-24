using AElf.Nethereum.Core;
using Nethereum.RPC.Eth.DTOs;
using Volo.Abp.DependencyInjection;

namespace AElf.Nethereum.Bridge;

public interface IBridgeInService
{
    Task<string> TransmitAsync(string chainId, string contractAddress, byte[] report, byte[][] rs, byte[][] ss, byte[] rawVs);
}

public class BridgeInService : ContractServiceBase, IBridgeInService, ITransientDependency
{
    protected override string SmartContractName { get; } = "BridgeIn";

    public async Task<string> TransmitAsync(string chainId, string contractAddress, byte[] report,
        byte[][] rs, byte[][] ss, byte[] rawVs)
    {
        var setValueFunction = GetFunction(chainId, contractAddress, "transmit");
        var sender = GetAccount().Address;
        var gas = await setValueFunction.EstimateGasAsync(sender, null, null, report, rs, ss, rawVs);
        var transactionResult =
            await setValueFunction.SendTransactionAsync(sender, gas, null, null, report,
                rs, ss, rawVs);
        return transactionResult;
    }
}