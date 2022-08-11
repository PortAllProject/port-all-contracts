using AElf.Client.Dto;

namespace AElf.Client;

public partial class AElfClient : IChainAppService
{
    /// <summary>
    /// Get the current status of the block chain.
    /// </summary>
    /// <returns>Description of current status</returns>
    public async Task<ChainStatusDto> GetChainStatusAsync()
    {
        var url = GetRequestUrl(_baseUrl, "api/blockChain/chainStatus");
        var chainStatus = await _httpService.GetResponseAsync<ChainStatusDto>(url);
        if (chainStatus == null)
        {
            throw new AElfClientException("Failed to get chain status");
        }

        return chainStatus;
    }

    /// <summary>
    /// Get the definitions of proto-buff related to a contract.
    /// </summary>
    /// <param name="address"></param>
    /// <returns>Definitions of proto-buff</returns>
    public async Task<byte[]> GetContractFileDescriptorSetAsync(string? address)
    {
        AssertValidAddress(address);
        var url = GetRequestUrl(_baseUrl, $"api/blockChain/contractFileDescriptorSet?address={address}");
        var set = await _httpService.GetResponseAsync<byte[]>(url);
        if (set == null)
        {
            throw new AElfClientException("Failed to get chain status");
        }

        return set;
    }

    /// <summary>
    /// Gets the status information of the task queue.
    /// </summary>
    /// <returns>Information of the task queue</returns>
    public async Task<List<TaskQueueInfoDto>> GetTaskQueueStatusAsync()
    {
        var url = GetRequestUrl(_baseUrl, "api/blockChain/taskQueueStatus");

        var taskQueueInfoList = await _httpService.GetResponseAsync<List<TaskQueueInfoDto>>(url);
        if (taskQueueInfoList == null)
        {
            throw new AElfClientException("Failed to get chain status");
        }

        return taskQueueInfoList;
    }

    /// <summary>
    /// Get id of the chain.
    /// </summary>
    /// <returns>ChainId</returns>
    public async Task<int> GetChainIdAsync()
    {
        var chainStatus = await GetChainStatusAsync();
        var base58ChainId = chainStatus.ChainId;
        var chainId = ChainHelper.ConvertBase58ToChainId(base58ChainId);
        return chainId;
    }
}