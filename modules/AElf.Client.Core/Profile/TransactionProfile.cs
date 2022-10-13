using AElf.Client.Dto;
using AutoMapper;
using Google.Protobuf;
using Volo.Abp.AutoMapper;

namespace AElf.Client.Core;

public class TransactionProfile : Profile
{
    public const string ErrorTrace = "WithMetrics";

    public TransactionProfile()
    {
        CreateMap<Transaction, TransactionDto>();
        CreateMap<TransactionDto, Transaction>();

        CreateMap<TransactionResult, TransactionResultDto>()
            .ForMember(d => d.ReturnValue, opt => opt.MapFrom(s => s.ReturnValue.ToHex(false)))
            .ForMember(d => d.Bloom,
                opt => opt.MapFrom(s =>
                    s.Status == TransactionResultStatus.NotExisted
                        ? null
                        : s.Bloom.Length == 0
                            ? ByteString.CopyFrom(new byte[256]).ToBase64()
                            : s.Bloom.ToBase64()))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString().ToUpper()))
            .ForMember(d => d.Error, opt => opt.MapFrom<TransactionErrorResolver>())
            .Ignore(d => d.Transaction)
            .Ignore(d => d.TransactionSize);

        TransactionResultStatus status;
        CreateMap<TransactionResultDto, TransactionResult>()
            .ForMember(d => d.ReturnValue,
                opt => opt.MapFrom(s => ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(s.ReturnValue))))
            .ForMember(d => d.BlockHash, opt => opt.MapFrom(s => Hash.LoadFromHex(s.BlockHash)))
            .ForMember(d => d.Bloom, opt => opt.MapFrom(s =>
                s.Status.ToUpper() == TransactionResultStatus.NotExisted.ToString().ToUpper()
                    ? null
                    : string.IsNullOrEmpty(s.Bloom)
                        ? ByteString.Empty
                        : ByteString.FromBase64(s.Bloom)))
            .ForMember(d => d.Status,
                opt => opt.MapFrom(s =>
                    Enum.TryParse($"{s.Status[0]}{s.Status.Substring(1).ToLower()}", out status)
                        ? status
                        : TransactionResultStatus.NotExisted))
            .ForMember(d => d.Logs, opt => opt.MapFrom(s => s.Logs))
            .Ignore(d => d.Error)
            .Ignore(d => d.Bloom);

        CreateMap<LogEventDto, LogEvent>()
            .ForMember(d => d.Indexed, opt => opt.MapFrom(s => s.Indexed.Select(ByteString.FromBase64)))
            .ForMember(d => d.NonIndexed, opt => opt.MapFrom(s => ByteString.FromBase64(s.NonIndexed)));
        CreateMap<LogEvent, LogEventDto>();
    }
}

public class TransactionErrorResolver : IValueResolver<TransactionResult, TransactionResultDto, string>
{
    public string Resolve(TransactionResult source, TransactionResultDto destination, string destMember,
        ResolutionContext context)
    {
        var errorTraceNeeded = (bool)context.Items[TransactionProfile.ErrorTrace];
        return TakeErrorMessage(source.Error, errorTraceNeeded);
    }

    public static string TakeErrorMessage(string transactionResultError, bool errorTraceNeeded)
    {
        if (string.IsNullOrWhiteSpace(transactionResultError))
            return null;

        if (errorTraceNeeded)
            return transactionResultError;

        using var stringReader = new StringReader(transactionResultError);
        return stringReader.ReadLine();
    }
}