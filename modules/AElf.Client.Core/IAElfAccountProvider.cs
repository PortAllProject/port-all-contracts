using AElf.Client.Core.Infrastructure;
using AElf.Client.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Nethereum.KeyStore;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Client.Core;

public interface IAElfAccountProvider
{
    byte[] GetPrivateKey(string? alias = null, string? address = null);
    void SetPrivateKey(byte[] privateKey, string? alias = null, string? address = null);
    void SetPrivateKey(string address, string password, string? alias = null);
    string GetDefaultPassword();
}

public class AElfAccountProvider : Dictionary<AElfAccountInfo, byte[]>, IAElfAccountProvider, ISingletonDependency
{
    private readonly IKeyDirectoryProvider _keyDirectoryProvider;
    private readonly AElfAccountOptions _aelfAccountOptions;
    private readonly string _aelfMinerAccountPassword;

    private readonly KeyStoreService _keyStoreService; 

    public ILogger<AElfAccountProvider> Logger { get; set; }

    public AElfAccountProvider(IKeyDirectoryProvider keyDirectoryProvider,
        IOptionsSnapshot<AElfAccountOptions> aelfAccountOptions,
        IOptionsSnapshot<AElfMinerAccountOptions> aelfMinerAccountOptions)
    {
        Logger = NullLogger<AElfAccountProvider>.Instance;
        _keyDirectoryProvider = keyDirectoryProvider;
        _aelfAccountOptions = aelfAccountOptions.Value;
        _aelfMinerAccountPassword = aelfMinerAccountOptions.Value.DefaultPassword;
        var defaultPrivateKey = ByteArrayHelper.HexStringToByteArray(AElfClientConstants.DefaultPrivateKey);
        SetPrivateKey(defaultPrivateKey, "Default", Address.FromPublicKey(defaultPrivateKey).ToBase58()); 
        _keyStoreService = new KeyStoreService();

        foreach (var accountConfig in aelfAccountOptions.Value.AccountConfigList)
        {
            if (string.IsNullOrWhiteSpace(accountConfig.PrivateKey))
            {
                var keyFilePath = GetKeyFileFullPath(accountConfig.Address, aelfAccountOptions.Value.KeyDirectory);
                var privateKey = AsyncHelper.RunSync(() => Task.Run(() =>
                {
                    using var textReader = File.OpenText(keyFilePath);
                    var json = textReader.ReadToEnd();
                    return _keyStoreService.DecryptKeyStoreFromJson(accountConfig.Password, json);
                }));
                SetPrivateKey(privateKey, accountConfig.Alias, accountConfig.Address);
            }
            else
            {
                var privateKey = ByteArrayHelper.HexStringToByteArray(accountConfig.PrivateKey);
                SetPrivateKey(privateKey, accountConfig.Alias, Address.FromPublicKey(privateKey).ToBase58());
            }
        }
    }

    public string GetDefaultPassword()
    {
        return _aelfMinerAccountPassword;
    }

    public byte[] GetPrivateKey(string? alias = null, string? address = null)
    {
        var keys = Keys
            .WhereIf(!alias.IsNullOrWhiteSpace(), a => a.Alias == alias)
            .WhereIf(!address.IsNullOrWhiteSpace(), a => a.Address == address)
            .ToList();
        if (keys.Count != 1)
        {
            throw new AElfClientException($"Failed to get private key of {alias} - {address}.");
        }

        return this[keys.Single()];
    }

    public void SetPrivateKey(byte[] privateKey, string? alias = null, string? address = null)
    {
        TryAdd(new AElfAccountInfo
        {
            Alias = alias,
            Address = address
        }, privateKey);
    }

    public void SetPrivateKey(string address, string password, string? alias = null)
    {
        var keyFilePath = GetKeyFileFullPath(address, _aelfAccountOptions.KeyDirectory);
        var privateKey = AsyncHelper.RunSync(() => Task.Run(() =>
        {
            using var textReader = File.OpenText(keyFilePath);
            var json = textReader.ReadToEnd();
            return _keyStoreService.DecryptKeyStoreFromJson(password, json);
        }));
        
        var keys = Keys
            .WhereIf(!alias.IsNullOrWhiteSpace(), a => a.Alias == alias)
            .WhereIf(!address.IsNullOrWhiteSpace(), a => a.Address == address)
            .ToList();

        if (keys.Count == 1) return;
        TryAdd(new AElfAccountInfo
        {
            Alias = alias,
            Address = address
        }, privateKey);
    }

    private string GetKeyFileFullPath(string address, string configuredKeyDirectory)
    {
        var dirPath = GetKeystoreDirectoryPath(configuredKeyDirectory);
        var filePath = Path.Combine(dirPath, address);
        var filePathWithExtension = Path.ChangeExtension(filePath, ".json");
        return filePathWithExtension;
    }

    private string GetKeystoreDirectoryPath(string? configuredKeyDirectory)
    {
        return string.IsNullOrWhiteSpace(configuredKeyDirectory)
            ? Path.Combine(_keyDirectoryProvider.GetAppDataPath(), "keys")
            : configuredKeyDirectory;
    }
}

public class AElfAccountInfo
{
    public string? Alias { get; set; }
    public string? Address { get; set; }
}