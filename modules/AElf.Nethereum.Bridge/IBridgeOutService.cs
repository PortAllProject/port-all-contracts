﻿using AElf.Nethereum.Core;
using Nethereum.RPC.Eth.DTOs;
using Volo.Abp.DependencyInjection;

namespace AElf.Nethereum.Bridge;

public interface IBridgeOutService
{
    Task<GetReceiptInfosDTO> GetSendReceiptInfosAsync(string clientAlias, string contractAddress, string token, long fromIndex);

    Task<GetSendReceiptIndexDTO> GetTransferReceiptIndexAsync(string clientAlias, string contractAddress, List<string> tokens,
        List<string> targetChainIds);
}

public class BridgeOutService : ContractServiceBase, IBridgeOutService, ITransientDependency
{
    protected override string SmartContractName { get; } = "BridgeOut";

    public async Task<GetReceiptInfosDTO> GetSendReceiptInfosAsync(string clientAlias, string contractAddress,
        string token, long fromIndex)
    {
        var function = GetFunction(clientAlias, contractAddress, "getSendReceiptInfos");

        var evmGetReceiptInfos = await function.CallDeserializingToObjectAsync<GetReceiptInfosDTO>(token, fromIndex);
        return evmGetReceiptInfos;
    }

    public async Task<GetSendReceiptIndexDTO> GetTransferReceiptIndexAsync(string clientAlias, string contractAddress,
        List<string> tokens, List<string> targetChainIds)
    {
        var function = GetFunction(clientAlias, contractAddress, "getSendReceiptIndex");

        var evmGetReceiptInfos =
            await function.CallDeserializingToObjectAsync<GetSendReceiptIndexDTO>(tokens, targetChainIds);
        return evmGetReceiptInfos;
    }
}