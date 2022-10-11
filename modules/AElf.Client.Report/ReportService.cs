using AElf.Client.Core;
using AElf.Client.Core.Options;
using AElf.Contracts.Report;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Client.Report;

public interface IReportService
{
    Task<SendTransactionResult> ProposeReportAsync(string chainId, CallbackInput proposeReportInput);

    Task<SendTransactionResult> ConfirmReportAsync(string chainId, ConfirmReportInput confirmReportInput);

    Task<SendTransactionResult> RejectReportAsync(string chainId, RejectReportInput rejectReportInput);

    Task<StringValue> GetRawReportAsync(string chainId, GetRawReportInput getRawReportInput);

    Task<Contracts.Report.Report> GetReportAsync(string chainId, GetReportInput getReportInput);
}

public class ReportService : ContractServiceBase, IReportService, ITransientDependency
{
    private readonly IAElfClientService _clientService;
    protected override string SmartContractName { get; } = "ReportContract";

    public ReportService(IAElfClientService clientService)
    {
        _clientService = clientService;
    }

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

    public async Task<StringValue> GetRawReportAsync(string chainId, GetRawReportInput getRawReportInput)
    {
        var result =
            await _clientService.ViewAsync(GetContractAddress(chainId), "GetRawReport",
                getRawReportInput, AElfChainAliasOptions.Value.Mapping[chainId]);
        var actualResult = new StringValue();
        actualResult.MergeFrom(result);
        return actualResult;
    }

    public async Task<Contracts.Report.Report> GetReportAsync(string chainId, GetReportInput getReportInput)
    {
        var result =
            await _clientService.ViewAsync(GetContractAddress(chainId), "GetReport",
                getReportInput, AElfChainAliasOptions.Value.Mapping[chainId]);
        var actualResult = new Contracts.Report.Report();
        actualResult.MergeFrom(result);
        return actualResult;
    }
}