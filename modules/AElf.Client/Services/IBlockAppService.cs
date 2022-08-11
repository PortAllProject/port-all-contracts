using AElf.Client.Dto;

namespace AElf.Client.Services;

public interface IBlockAppService
{
    Task<long> GetBlockHeightAsync();

    Task<BlockDto?> GetBlockByHashAsync(string blockHash, bool includeTransactions = false);

    Task<BlockDto?> GetBlockByHeightAsync(long blockHeight, bool includeTransactions = false);
}