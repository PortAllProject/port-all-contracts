using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Contracts.Report;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Report;

public interface IReportService
{
    Task<SendTransactionResult> ProposeReportAsync(string clientAlias, CallbackInput proposeReportInput);

    Task<SendTransactionResult> ConfirmReportAsync(string clientAlias, ConfirmReportInput confirmReportInput);

    Task<SendTransactionResult> RejectReportAsync(string clientAlias, RejectReportInput rejectReportInput);
}

public class ReportService : ContractServiceBase, IReportService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    private readonly AElfClientConfigOptions _clientConfigOptions;

    protected override string SmartContractName { get; } = "ReportContract";

    public ReportService(IAElfClientService clientService,
        IOptionsSnapshot<AElfClientConfigOptions> clientConfigOptions,
        IOptionsSnapshot<AElfContractOptions> contractOptions)
    {
        _clientService = clientService;
        _clientConfigOptions = clientConfigOptions.Value;
    }

    public async Task<SendTransactionResult> ProposeReportAsync(string clientAlias, CallbackInput proposeReportInput)
    {
        var tx = await PerformSendTransactionAsync("ProposeReport", proposeReportInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<SendTransactionResult> ConfirmReportAsync(string clientAlias, ConfirmReportInput confirmReportInput)
    {
        var tx = await PerformSendTransactionAsync("ConfirmReport", confirmReportInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    public async Task<SendTransactionResult> RejectReportAsync(string clientAlias, RejectReportInput rejectReportInput)
    {
        var tx = await PerformSendTransactionAsync("RejectReport", rejectReportInput, clientAlias);
        return new SendTransactionResult
        {
            Transaction = tx,
            TransactionResult = await PerformGetTransactionResultAsync(tx.GetHash().ToHex(), clientAlias)
        };
    }

    
}