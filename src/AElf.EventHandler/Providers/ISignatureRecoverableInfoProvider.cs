using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler;

public interface ISignatureRecoverableInfoProvider
{
    Task SetSignatureAsync(string chainId, string ethereumContractAddress, long roundId, string recoverableInfo);
    Task<HashSet<string>> GetSignatureAsync(string chainId, string ethereumContractAddress, long roundId);
    Task RemoveSignatureAsync(string chainId, string ethereumContractAddress, long roundId);
}

public class SignatureRecoverableInfoProvider : AbpRedisCache, ISignatureRecoverableInfoProvider, ISingletonDependency
{
    private ILogger<SignatureRecoverableInfoProvider> _logger;
    private readonly IDistributedCacheSerializer _serializer;

    private const string KeyPrefix = "ReportSignature";

    public SignatureRecoverableInfoProvider(ILogger<SignatureRecoverableInfoProvider> logger,
        IOptions<RedisCacheOptions> optionsAccessor, IDistributedCacheSerializer serializer) : base(optionsAccessor)
    {
        _serializer = serializer;
        _logger = logger;
    }

    public async Task SetSignatureAsync(string chainId, string ethereumContractAddress, long roundId,
        string recoverableInfo)
    {
        var key = GetStoreKey(chainId, ethereumContractAddress, roundId);
        var signatureBytes = await GetAsync(key);
        ReportSignature signature;
        if (signatureBytes == null)
        {
            signature = new ReportSignature
            {
                Address = ethereumContractAddress,
                RoundId = roundId,
                Signatures = new HashSet<string>()
            };
        }
        else
        {
            signature = _serializer.Deserialize<ReportSignature>(signatureBytes);
        }

        signature.Signatures.Add(recoverableInfo);

        await SetAsync(key, _serializer.Serialize(signature), new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = DateTimeOffset.MaxValue
        });
    }

    public async Task<HashSet<string>> GetSignatureAsync(string chainId, string ethereumContractAddress, long roundId)
    {
        var key = GetStoreKey(chainId, ethereumContractAddress, roundId);
        var signatureBytes = await GetAsync(key);
        var signature = _serializer.Deserialize<ReportSignature>(signatureBytes);
        return signature.Signatures;
    }

    public async Task RemoveSignatureAsync(string chainId, string ethereumContractAddress, long roundId)
    {
        var key = GetStoreKey(chainId, ethereumContractAddress, roundId);
        await RemoveAsync(key);
    }

    private string GetStoreKey(string chainId, string ethereumContractAddress, long roundId)
    {
        return $"{KeyPrefix}-{chainId}-{ethereumContractAddress}-{roundId}";
    }
}

public class ReportSignature
{
    public string Address { get; set; }
    public long RoundId { get; set; }
    public HashSet<string> Signatures { get; set; } = new();
}