using System.Net.Http.Headers;
using System.Text;
using AElf.Client.Dto;
using AElf.Client.Helper;
using AElf.Client.Services;

namespace AElf.Client;

public partial class AElfClient : INetAppService
{
    /// <summary>
    /// Attempt to add a node to the connected network nodes.Input parameter contains the ipAddress of the node.
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <returns>Add successfully or not</returns>
    public async Task<bool> AddPeerAsync(string ipAddress, string? userName, string? password)
    {
        if (!EndpointHelper.TryParse(ipAddress, out var endpoint))
        {
            return false;
        }

        var url = GetRequestUrl(_baseUrl, "api/net/peer");
        var parameters = new Dictionary<string, string>
        {
            { "address", endpoint?.ToString() ?? AElfClientConstants.LocalEndpoint },
        };

        return await _httpService.PostResponseAsync<bool>(url, parameters,
            authenticationHeaderValue: GetAuthenticationHeaderValue(userName, password));
    }

    /// <summary>
    /// Attempt to remove a node from the connected network nodes by given the ipAddress.
    /// </summary>
    /// <param name="ipAddress"></param>
    /// <param name="userName"></param>
    /// <param name="password"></param>
    /// <returns>Delete successfully or not</returns>
    public async Task<bool> RemovePeerAsync(string ipAddress, string? userName, string? password)
    {
        if (!EndpointHelper.TryParse(ipAddress, out var endpoint))
        {
            return false;
        }

        var url = GetRequestUrl(_baseUrl, $"api/net/peer?address={endpoint}");
        return await _httpService.DeleteResponseAsObjectAsync<bool>(url,
            authenticationHeaderValue: GetAuthenticationHeaderValue(userName, password));
    }

    /// <summary>
    /// Gets information about the peer nodes of the current node.Optional whether to include metrics.
    /// </summary>
    /// <param name="withMetrics"></param>
    /// <returns>Information about the peer nodes</returns>
    public async Task<List<PeerDto>?> GetPeersAsync(bool withMetrics)
    {
        var url = GetRequestUrl(_baseUrl, $"api/net/peers?withMetrics={withMetrics}");
        return await _httpService.GetResponseAsync<List<PeerDto>>(url);
    }

    /// <summary>
    /// Get the node's network information.
    /// </summary>
    /// <returns>Network information</returns>
    public async Task<NetworkInfoOutput?> GetNetworkInfoAsync()
    {
        var url = GetRequestUrl(_baseUrl, "api/net/networkInfo");
        return await _httpService.GetResponseAsync<NetworkInfoOutput>(url);
    }

    private AuthenticationHeaderValue GetAuthenticationHeaderValue(string? userName, string? password)
    {
        var byteArray = Encoding.ASCII.GetBytes($"{userName ?? _userName}:{password ?? _password}");
        return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
    }
}