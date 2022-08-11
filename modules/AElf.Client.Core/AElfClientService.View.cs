using AElf.Client.Dto;
using AElf.Client.Core.Options;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElf.Client.Core;

public partial class AElfClientService : IAElfClientService, ITransientDependency
{
    private readonly IAElfClientProvider _aelfClientProvider;
    private readonly IAElfAccountProvider _aelfAccountProvider;
    private readonly IObjectMapper<AElfClientModule> _objectMapper;
    private readonly AElfClientConfigOptions _clientConfigOptions;

    public ILogger<AElfClientService> Logger { get; set; }

    public AElfClientService(IAElfClientProvider aelfClientProvider, IAElfAccountProvider aelfAccountProvider,
        IObjectMapper<AElfClientModule> objectMapper, IOptionsSnapshot<AElfClientConfigOptions> clientConfigOptions)
    {
        _aelfClientProvider = aelfClientProvider;
        _aelfAccountProvider = aelfAccountProvider;
        _objectMapper = objectMapper;
        _clientConfigOptions = clientConfigOptions.Value;

        Logger = NullLogger<AElfClientService>.Instance;
    }

    public async Task<byte[]> ViewAsync(string contractAddress, string methodName, IMessage parameter,
        string clientAlias, string accountAlias = "Default")
    {
        var aelfClient = _aelfClientProvider.GetClient(alias: clientAlias);
        var aelfAccount = _aelfAccountProvider.GetPrivateKey(alias: accountAlias);
        var tx = new TransactionBuilder(aelfClient)
            .UsePrivateKey(aelfAccount)
            .UseContract(contractAddress)
            .UseMethod(methodName)
            .UseParameter(parameter)
            .Build();
        return await PerformViewAsync(aelfClient, tx);
    }

    public async Task<byte[]> ViewSystemAsync(string systemContractName, string methodName, IMessage parameter,
        string clientAlias, string accountAlias = "Default")
    {
        var aelfClient = _aelfClientProvider.GetClient(alias: clientAlias);
        var privateKey = _aelfAccountProvider.GetPrivateKey(alias: accountAlias);
        var tx = new TransactionBuilder(aelfClient)
            .UsePrivateKey(privateKey)
            .UseSystemContract(systemContractName)
            .UseMethod(methodName)
            .UseParameter(parameter)
            .Build();
        return await PerformViewAsync(aelfClient, tx);
    }

    private async Task<byte[]> PerformViewAsync(AElfClient aelfClient, Transaction tx)
    {
        var result = await aelfClient.ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = tx.ToByteArray().ToHex()
        });
        return ByteArrayHelper.HexStringToByteArray(result);
    }
}