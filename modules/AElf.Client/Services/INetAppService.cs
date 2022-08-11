using AElf.Client.Dto;

namespace AElf.Client.Services;

public interface INetAppService
{
    Task<bool> AddPeerAsync(string ipAddress, string userName, string password);

    Task<bool> RemovePeerAsync(string ipAddress, string userName, string password);

    Task<List<PeerDto>?> GetPeersAsync(bool withMetrics);

    Task<NetworkInfoOutput?> GetNetworkInfoAsync();
}