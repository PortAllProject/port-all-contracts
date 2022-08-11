using AElf.Client.Dto;
using Castle.Core.Logging;
using Google.Protobuf;

namespace AElf.Client.Core;

public partial class AElfClientService
{
    public async Task<Transaction> SendAsync(string contractAddress, string methodName, IMessage parameter,
        string clientAlias, string? alias = null, string? address = null)
    {
        var aelfClient = _aelfClientProvider.GetClient(alias: clientAlias);
        var aelfAccount = SetAccount(alias, address);
        var tx = new TransactionBuilder(aelfClient)
            .UsePrivateKey(aelfAccount)
            .UseContract(contractAddress)
            .UseMethod(methodName)
            .UseParameter(parameter)
            .Build();
        await PerformSendAsync(aelfClient, tx);
        return tx;
    }

    public async Task<Transaction> SendSystemAsync(string systemContractName, string methodName, IMessage parameter,
        string clientAlias, string? alias = null, string? address = null)
    {
        var aelfClient = _aelfClientProvider.GetClient(alias: clientAlias);
        var aelfAccount = SetAccount(alias, address);
        var tx = new TransactionBuilder(aelfClient)
            .UsePrivateKey(aelfAccount)
            .UseSystemContract(systemContractName)
            .UseMethod(methodName)
            .UseParameter(parameter)
            .Build();
        await PerformSendAsync(aelfClient, tx);
        return tx;
    }

    private static async Task PerformSendAsync(AElfClient aelfClient, Transaction tx)
    {
        var result = await aelfClient.SendTransactionAsync(new SendTransactionInput
        {
            RawTransaction = tx.ToByteArray().ToHex()
        });
    }

    private byte[] SetAccount(string? alias, string? address)
    {
        byte[] aelfAccount;
        if (!string.IsNullOrWhiteSpace(address))
        {
            _aelfAccountProvider.SetPrivateKey(address, _aelfAccountProvider.GetDefaultPassword());
            aelfAccount = _aelfAccountProvider.GetPrivateKey(null, address);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(alias))
                alias = _clientConfigOptions.AccountAlias;
            aelfAccount = _aelfAccountProvider.GetPrivateKey(alias);
        }

        return aelfAccount;
    }
}