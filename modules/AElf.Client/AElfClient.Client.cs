using AElf.Client.Dto;
using AElf.Client.Extensions;
using AElf.Client.Model;
using AElf.Client.Services;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Client;

public partial class AElfClient : IClientService
{
    /// <summary>
    /// Verify whether this sdk successfully connects the chain.
    /// </summary>
    /// <returns>IsConnected or not</returns>
    public async Task<bool> IsConnectedAsync()
    {
        try
        {
            await GetChainStatusAsync();
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Get the address of genesis contract.
    /// </summary>
    /// <returns>Address</returns>
    public async Task<string?> GetGenesisContractAddressAsync()
    {
        var statusDto = await GetChainStatusAsync();
        var genesisAddress = statusDto.GenesisContractAddress;
        return genesisAddress;
    }

    /// <summary>
    /// Get address of a contract by given contractNameHash.
    /// </summary>
    /// <param name="contractNameHash"></param>
    /// <returns>Address</returns>
    public async Task<Address> GetContractAddressByNameAsync(Hash contractNameHash)
    {
        var from = GetAddressFromPrivateKey(AElfClientConstants.DefaultPrivateKey);
        var to = await GetGenesisContractAddressAsync();
        var transaction = await GenerateTransactionAsync(from, to, "GetContractAddressByName", contractNameHash);
        var txWithSig = SignTransaction(AElfClientConstants.DefaultPrivateKey, transaction);

        var response = await ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSig.ToByteArray().ToHex()
        });
        var byteArray = ByteArrayHelper.HexStringToByteArray(response);
        var address = Address.Parser.ParseFrom(byteArray);

        return address;
    }

    /// <summary>
    /// Build a transaction from the input parameters.
    /// </summary>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="methodName"></param>
    /// <param name="input"></param>
    /// <returns>Transaction unsigned</returns>
    public async Task<Transaction> GenerateTransactionAsync(string? from, string? to,
        string methodName, IMessage input)
    {
        try
        {
            AssertValidAddress(to);
            var chainStatus = await GetChainStatusAsync();
            var transaction = new Transaction
            {
                From = from.ToAddress(),
                To = Address.FromBase58(to),
                MethodName = methodName,
                Params = input.ToByteString(),
                RefBlockNumber = chainStatus.BestChainHeight,
                RefBlockPrefix = ByteString.CopyFrom(Hash.LoadFromHex(chainStatus.BestChainHash).Value
                    .Take(4).ToArray())
            };

            return transaction;
        }
        catch (Exception ex)
        {
            throw new AElfClientException($"Failed to generate transaction: {ex.Message}");
        }
    }

    /// <summary>
    /// Convert the Address to the displayed string：symbol_base58-string_base58-string-chain-id
    /// </summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public async Task<string> GetFormattedAddressAsync(Address address)
    {
        var tokenContractAddress =
            await GetContractAddressByNameAsync(HashHelper.ComputeFrom("AElf.ContractNames.Token"));
        var fromAddress = GetAddressFromPrivateKey(AElfClientConstants.DefaultPrivateKey);
        var toAddress = tokenContractAddress.ToBase58();
        var methodName = "GetPrimaryTokenSymbol";
        var param = new Empty();

        var transaction = await GenerateTransactionAsync(fromAddress, toAddress, methodName, param);
        var txWithSign = SignTransaction(AElfClientConstants.DefaultPrivateKey, transaction);

        var result = await ExecuteTransactionAsync(new ExecuteTransactionDto
        {
            RawTransaction = txWithSign.ToByteArray().ToHex()
        });

        var symbol = StringValue.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(result));
        var chainIdString = (await GetChainStatusAsync()).ChainId;

        return $"{symbol.Value}_{address.ToBase58()}_{chainIdString}";
    }

    /// <summary>
    /// Sign a transaction using private key.
    /// </summary>
    /// <param name="privateKeyHex"></param>
    /// <param name="transaction"></param>
    /// <returns>Transaction signed</returns>
    public Transaction SignTransaction(string? privateKeyHex, Transaction transaction)
    {
        var transactionData = transaction.GetHash().ToByteArray();

        privateKeyHex ??= AElfClientConstants.DefaultPrivateKey;

        // Sign the hash
        var privateKey = ByteArrayHelper.HexStringToByteArray(privateKeyHex);
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, transactionData);
        transaction.Signature = ByteString.CopyFrom(signature);

        return transaction;
    }

    /// <summary>
    /// Sign a transaction using private key.
    /// </summary>
    /// <param name="privateKey"></param>
    /// <param name="transaction"></param>
    /// <returns>Transaction signed</returns>
    public Transaction SignTransaction(byte[]? privateKey, Transaction transaction)
    {
        var transactionData = transaction.GetHash().ToByteArray();

        privateKey ??= ByteArrayHelper.HexStringToByteArray(AElfClientConstants.DefaultPrivateKey);

        // Sign the hash
        var signature = CryptoHelper.SignWithPrivateKey(privateKey, transactionData);
        transaction.Signature = ByteString.CopyFrom(signature);

        return transaction;
    }

    /// <summary>
    /// Get the account address through the public key.
    /// </summary>
    /// <param name="pubKey"></param>
    /// <returns>Account</returns>
    public string GetAddressFromPubKey(string pubKey)
    {
        var publicKey = ByteArrayHelper.HexStringToByteArray(pubKey);
        var address = Address.FromPublicKey(publicKey);
        return address.ToBase58();
    }

    /// <summary>
    /// Get the account address through the private key.
    /// </summary>
    /// <param name="privateKeyHex"></param>
    /// <returns></returns>
    public string? GetAddressFromPrivateKey(string? privateKeyHex)
    {
        var address = Address.FromPublicKey(GetAElfKeyPair(privateKeyHex).PublicKey);
        return address.ToBase58();
    }

    public KeyPairInfo GenerateKeyPairInfo()
    {
        var keyPair = CryptoHelper.GenerateKeyPair();
        var privateKey = keyPair.PrivateKey.ToHex();
        var publicKey = keyPair.PublicKey.ToHex();
        var address = GetAddressFromPrivateKey(privateKey);

        return new KeyPairInfo
        {
            PrivateKey = privateKey,
            PublicKey = publicKey,
            Address = address
        };
    }

    #region private methods

    private ECKeyPair GetAElfKeyPair(string? privateKeyHex)
    {
        var privateKey = ByteArrayHelper.HexStringToByteArray(privateKeyHex);
        var keyPair = CryptoHelper.FromPrivateKey(privateKey);

        return keyPair;
    }

    private string GetRequestUrl(string baseUrl, string relativeUrl)
    {
        var uri = new Uri(baseUrl + (baseUrl.EndsWith("/") ? "" : "/"));
        return new Uri(uri, relativeUrl).ToString();
    }

    private void AssertValidAddress(params string?[] addresses)
    {
        try
        {
            foreach (var address in addresses)
            {
                Address.FromBase58(address);
            }
        }
        catch (Exception)
        {
            throw new AElfClientException(Error.Message[Error.InvalidAddress]);
        }
    }

    private void AssertValidHash(params string[] hashes)
    {
        try
        {
            foreach (var hash in hashes)
            {
                Hash.LoadFromHex(hash);
            }
        }
        catch (Exception)
        {
            throw new AElfClientException(Error.Message[Error.InvalidBlockHash]);
        }
    }

    private void AssertValidTransactionId(params string[] transactionIds)
    {
        try
        {
            foreach (var transactionId in transactionIds)
            {
                Hash.LoadFromHex(transactionId);
            }
        }
        catch (Exception)
        {
            throw new AElfClientException(Error.Message[Error.InvalidTransactionId]);
        }
    }

    #endregion
}