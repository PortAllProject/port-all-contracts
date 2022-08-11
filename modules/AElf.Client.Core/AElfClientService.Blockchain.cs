using AElf.Client.Dto;
using Microsoft.Extensions.Logging;

namespace AElf.Client.Core;

public partial class AElfClientService
{
    public async Task<TransactionResult> GetTransactionResultAsync(string transactionId, string clientAlias)
    {
        var aelfClient = _aelfClientProvider.GetClient(alias: clientAlias);
        var result = await aelfClient.GetTransactionResultAsync(transactionId);
        var i = 0;
        while (i < 10)
        {
            if (result!.Status == TransactionResultStatus.Mined.ToString().ToUpper())
            {
                break;
            }

            if (result.Status == TransactionResultStatus.Failed.ToString().ToUpper() ||
                result.Status == TransactionResultStatus.NodeValidationFailed.ToString().ToUpper())
            {
                break;
            }

            await Task.Delay(AElfClientAbpConstants.DefaultWaitMilliseconds);
            result = await aelfClient.GetTransactionResultAsync(transactionId);
            i++;
        }

        return _objectMapper.Map<TransactionResultDto, TransactionResult>(result!);
    }

    public async Task<ChainStatusDto> GetChainStatusAsync(string clientAlias)
    {
        var aelfClient = _aelfClientProvider.GetClient(alias: clientAlias);
        return await aelfClient.GetChainStatusAsync();
    }

    public async Task<MerklePath> GetMerklePathByTransactionIdAsync(string transactionId, string clientAlias)
    {
        var aelfClient = _aelfClientProvider.GetClient(alias: clientAlias);
        var merklePathDto = await aelfClient.GetMerklePathByTransactionIdAsync(transactionId);
        if (merklePathDto == null)
        {
            Logger.LogError("Cannot get merkle path of transaction {TransactionId}", transactionId);
        }

        return _objectMapper.Map<MerklePathDto, MerklePath>(merklePathDto);
    }
}