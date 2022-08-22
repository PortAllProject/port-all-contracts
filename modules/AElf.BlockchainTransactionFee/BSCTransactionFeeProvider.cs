using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.BlockchainTransactionFee;

public class BSCTransactionFeeProvider : IBlockchainTransactionFeeProvider
{
    public string BlockChain { get; } = "BSC";

    private readonly ApiClient _apiClient;

    private readonly ChainExplorerApiOptions _chainExplorerApiOptions;

    public BSCTransactionFeeProvider(ApiClient apiClient,
        IOptionsSnapshot<ChainExplorerApiOptions> blockchainExplorerApiOptions)
    {
        _apiClient = apiClient;
        _chainExplorerApiOptions = blockchainExplorerApiOptions.Value;
    }

    public async Task<TransactionFeeDto> GetTransactionFee()
    {
        var result = await _apiClient.GetAsync<BSCApiResult<BSCGasTracker>>(
            $"https://api.bscscan.com/api?module=gastracker&action=gasoracle&apikey={_chainExplorerApiOptions.ApiKeys[BlockChain]}");
        if (result.Message != "OK")
        {
            throw new HttpRequestException($"BSC api failed: {result.Message}");
        }

        return new TransactionFeeDto
        {
            Symbol = "BNB",
            Fee = decimal.Parse(result.Result.SafeGasPrice)
        };
    }
}

public class BSCApiResult<T>
{
    public string Message { get; set; }

    public T Result { get; set; }
}

public class BSCGasTracker
{
    public string FastGasPrice { get; set; }
    public string LastBlock { get; set; }
    public string ProposeGasPrice { get; set; }
    public string SafeGasPrice { get; set; }
    public string UsdPrice { get; set; }
}