using AElf.Client.Dto;

namespace AElf.Client.Services;

public interface ITransactionAppService
{
    Task<TransactionPoolStatusOutput?> GetTransactionPoolStatusAsync();

    Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input);

    Task<string?> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input);

    Task<CreateRawTransactionOutput?> CreateRawTransactionAsync(CreateRawTransactionInput input);

    Task<SendRawTransactionOutput?> SendRawTransactionAsync(SendRawTransactionInput input);

    Task<SendTransactionOutput?> SendTransactionAsync(SendTransactionInput input);

    Task<string[]?> SendTransactionsAsync(SendTransactionsInput input);

    Task<TransactionResultDto?> GetTransactionResultAsync(string transactionId);

    Task<List<TransactionResultDto>?> GetTransactionResultsAsync(string blockHash, int offset = 0,
        int limit = 10);

    Task<MerklePathDto?> GetMerklePathByTransactionIdAsync(string transactionId);
}