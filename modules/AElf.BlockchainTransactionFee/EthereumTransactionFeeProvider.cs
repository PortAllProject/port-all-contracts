using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.BlockchainTransactionFee;

public class EthereumTransactionFeeProvider : IBlockchainTransactionFeeProvider
{
    public string BlockChain { get; } = "Ethereum";

    private readonly ApiClient _apiClient;

    private readonly ChainExplorerApiOptions _chainExplorerApiOptions;

    public EthereumTransactionFeeProvider(ApiClient apiClient,
        IOptionsSnapshot<ChainExplorerApiOptions> blockchainExplorerApiOptions)
    {
        _apiClient = apiClient;
        _chainExplorerApiOptions = blockchainExplorerApiOptions.Value;
    }

    public async Task<TransactionFeeDto> GetTransactionFee()
    {
        var result = await _apiClient.GetAsync<EthereumApiResult<EthereumGasTracker>>(
            $"https://api.etherscan.io/api?module=gastracker&action=gasoracle&apikey={_chainExplorerApiOptions.ApiKeys[BlockChain]}");
        if (result.Message != "OK")
        {
            throw new HttpRequestException($"Ethereum api failed: {result.Message}");
        }

        return new TransactionFeeDto
        {
            Symbol = "ETH",
            Fee = decimal.Parse(result.Result.SafeGasPrice)
        };
    }
}

public class EthereumApiResult<T>
{
    public string Message { get; set; }

    public T Result { get; set; }
}

public class EthereumGasTracker
{
    public string FastGasPrice { get; set; }
    public string LastBlock { get; set; }
    public string ProposeGasPrice { get; set; }
    public string SafeGasPrice { get; set; }
}