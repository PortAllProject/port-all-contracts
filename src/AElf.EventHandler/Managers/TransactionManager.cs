using System;
using System.Linq;
using System.Threading;
using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.Threading;

namespace AElf.EventHandler
{
    public class TransactionManager
    {
        private readonly AElfKeyStore _keyStore;

        public TransactionManager(AElfKeyStore keyStore)
        {
            _keyStore = keyStore;
        }

        public Transaction CreateTransaction(string from, string to,
            string methodName, ByteString input)
        {
            try
            {
                var transaction = new Transaction
                {
                    From = from.ConvertAddress(),
                    To = to.ConvertAddress(),
                    MethodName = methodName,
                    Params = input ?? ByteString.Empty
                };

                return transaction;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public Transaction SignTransaction(Transaction tx)
        {
            if (!IsKeyReady())
            {
                throw new InvalidOperationException("Key not ready.");
            }
            var txData = tx.GetHash().ToByteArray();
            tx.Signature = GetSignature(txData);
            return tx;
        }

        private ByteString GetSignature(byte[] txData)
        {
            var kp = _keyStore.GetAccountKeyPair();

            if (kp == null)
            {
                return null;
            }

            // Sign the hash
            var signature = CryptoHelper.SignWithPrivateKey(kp.PrivateKey, txData);
            return ByteString.CopyFrom(signature);
        }

        public bool IsKeyReady()
        {
            return _keyStore.GetAccountKeyPair() != null;
        }

        public string ConvertTransactionRawTxString(Transaction tx)
        {
            return tx.ToByteArray().ToHex();
        }
    }

    public static class BlockMarkingHelper
    {
        private static DateTime _refBlockTime = DateTime.Now;
        private static long _cachedHeight;
        private static string _cachedHash;
        private static string _chainId = string.Empty;
        private static string _baseUrl = string.Empty;

        public static Transaction AddBlockReference(this Transaction transaction, string rpcAddress,
            string chainId = "AELF")
        {if (_cachedHeight == default || (DateTime.Now - _refBlockTime).TotalSeconds > 60 ||
                !_chainId.Equals(chainId)||_baseUrl !=rpcAddress)
            {
                _chainId = chainId;
                (_cachedHeight, _cachedHash) = GetBlockReference(rpcAddress);
                _refBlockTime = DateTime.Now;
                _baseUrl = rpcAddress;
            }

            transaction.RefBlockNumber = _cachedHeight;
            transaction.RefBlockPrefix =
                ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(_cachedHash).Where((b, i) => i < 4).ToArray());
            return transaction;
        }

        private static (long, string) GetBlockReference(string baseUrl, int requestTimes = 4)
        {
            while (true)
                try
                {
                    var client = AElfClientExtension.GetClient(baseUrl);
                    var chainStatus = AsyncHelper.RunSync(client.GetChainStatusAsync);
                    if (chainStatus.LongestChainHeight - chainStatus.LastIrreversibleBlockHeight > 400)
                    {
                        continue;
                    }

                    //return (chainStatus.LastIrreversibleBlockHeight, chainStatus.LastIrreversibleBlockHash);
                    return (chainStatus.BestChainHeight, chainStatus.BestChainHash);
                }
                catch (Exception)
                {
                    requestTimes--;
                    if (requestTimes < 0) throw new Exception("Get chain status got failed exception.");
                    Thread.Sleep(500);
                }
        }
    }
}