using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Report;

public interface IReportService
{
    Task<SendTransactionResult> ProposeReportAsync(string chainId, CallbackInput proposeReportInput);

    Task<SendTransactionResult> ConfirmReportAsync(string chainId, ConfirmReportInput confirmReportInput);

    Task<SendTransactionResult> RejectReportAsync(string chainId, RejectReportInput rejectReportInput);
}

public class ReportService : ContractServiceBase, IReportService, ITransientDependency
{
    protected override string SmartContractName { get; } = "ReportContract";

    public async Task<SendTransactionResult> ProposeReportAsync(string chainId, CallbackInput proposeReportInput)
    {
        var tx = await PerformSendTransactionAsync("ProposeReport", proposeReportInput, chainId);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), chainId)
        };
    }

    public async Task<SendTransactionResult> ConfirmReportAsync(string chainId, ConfirmReportInput confirmReportInput)
    {
        var tx = await PerformSendTransactionAsync("ConfirmReport", confirmReportInput, chainId);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), chainId)
        };
    }

    public async Task<SendTransactionResult> RejectReportAsync(string chainId, RejectReportInput rejectReportInput)
    {
        var tx = await PerformSendTransactionAsync("RejectReport", rejectReportInput, chainId);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), chainId)
        };
    }

    
}