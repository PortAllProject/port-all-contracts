using Nethereum.BlockchainProcessing.BlockStorage.Entities.Mapping;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Volo.Abp.DependencyInjection;

namespace AElf.Nethereum.Core;

public interface INethereumService
{
    Task<long> GetBlockNumberAsync(string clientAlias);
    Task<TransactionReceipt> GetTransactionReceiptAsync(string clientAlias, string transactionHash);
    Task<BlockWithTransactionHashes> GetBlockByNumberAsync(string clientAlias, HexBigInteger number);
}

public class NethereumService : INethereumService, ITransientDependency
{
    private readonly INethereumClientProvider _nethereumClientProvider;

    public NethereumService(INethereumClientProvider nethereumClientProvider)
    {
        _nethereumClientProvider = nethereumClientProvider;
    }

    public async Task<long> GetBlockNumberAsync(string clientAlias)
    {
        var web3 = _nethereumClientProvider.GetClient(clientAlias);
        var latestBlockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
        return latestBlockNumber.ToLong();
    }

    public async Task<TransactionReceipt> GetTransactionReceiptAsync(string clientAlias, string transactionHash)
    {
        var web3 = _nethereumClientProvider.GetClient(clientAlias);
        return await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);
    }
    
    public async Task<BlockWithTransactionHashes> GetBlockByNumberAsync(string clientAlias, HexBigInteger number)
    {
        var web3 = _nethereumClientProvider.GetClient(clientAlias);
        return await web3.Eth.Blocks.GetBlockWithTransactionsHashesByNumber.SendRequestAsync(number);
    }
}