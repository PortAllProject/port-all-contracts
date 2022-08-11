using AElf.Client.Dto;
using AElf.Contracts.MultiToken;
using Google.Protobuf;

namespace AElf.Client.Extensions;

public static class TransactionResultDtoExtension
{
    public static Dictionary<string, long> GetTransactionFees(this TransactionResultDto transactionResultDto)
    {
        var result = new Dictionary<string, long>();

        var transactionFeeLogs =
            transactionResultDto.Logs?.Where(l => l.Name == nameof(TransactionFeeCharged)).ToList();
        if (transactionFeeLogs != null)
        {
            foreach (var transactionFee in transactionFeeLogs.Select(transactionFeeLog =>
                         TransactionFeeCharged.Parser.ParseFrom(ByteString.FromBase64(transactionFeeLog.NonIndexed))))
            {
                result.Add(transactionFee.Symbol, transactionFee.Amount);
            }
        }

        var resourceTokenLogs =
            transactionResultDto.Logs?.Where(l => l.Name == nameof(ResourceTokenCharged)).ToList();
        if (resourceTokenLogs != null)
        {
            foreach (var resourceToken in resourceTokenLogs.Select(transactionFeeLog =>
                         ResourceTokenCharged.Parser.ParseFrom(ByteString.FromBase64(transactionFeeLog.NonIndexed))))
            {
                result.Add(resourceToken.Symbol, resourceToken.Amount);
            }
        }

        return result;
    }
}