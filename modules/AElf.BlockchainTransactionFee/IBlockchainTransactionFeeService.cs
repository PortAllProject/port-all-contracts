using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace AElf.BlockchainTransactionFee;

public interface IBlockchainTransactionFeeService
{
    Task<TransactionFeeDto> GetTransactionFeeAsync(string chainName);
}

public class BlockchainTransactionFeeService : IBlockchainTransactionFeeService, ITransientDependency
{
    private readonly IEnumerable<IBlockchainTransactionFeeProvider> _blockchainTransactionFeeProviders;

    public BlockchainTransactionFeeService(
        IEnumerable<IBlockchainTransactionFeeProvider> blockchainTransactionFeeProviders)
    {
        _blockchainTransactionFeeProviders = blockchainTransactionFeeProviders;
    }

    public async Task<TransactionFeeDto> GetTransactionFeeAsync(string chainName)
    {
        var provider = _blockchainTransactionFeeProviders.First(o => o.BlockChain == chainName);
        var fee = await provider.GetTransactionFee();
        return fee;
    }
}


