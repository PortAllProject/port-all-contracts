namespace AElf.BlockchainTransactionFee;

public interface IBlockchainTransactionFeeProvider
{
    string BlockChain { get; }
    Task<TransactionFeeDto> GetTransactionFee();
}