using System;
using System.Linq;
using AElf;
using AElf.Cryptography;
using Org.BouncyCastle.Crypto.Digests;

namespace ReportGenerator
{
    public class Signature
    {
        public string HashMsg { get; set; }
        public string RecoverInfo { get; set; }
        public string R { get; set; }
        public string S { get; set; }
        public string V { get; set; }
    }
    public class SignService
    {
        public string GenerateAddressOnEthereum(byte[] publicKey)
        {
            var publicKeyStr = publicKey.ToHex();
            if (publicKeyStr.StartsWith("0x"))
            {
                publicKeyStr = publicKeyStr.Substring(2, publicKey.Length - 2);
            }
            publicKeyStr = publicKeyStr.Substring(2, publicKey.Length - 2);
            publicKeyStr = GetKeccak256(publicKeyStr);
            var address = "0x" + publicKeyStr.Substring(publicKey.Length - 40, 40);
            return address;
        }
        
        public string GenerateAddressOnEthereum(string publicKey)
        {
            if (publicKey.StartsWith("0x"))
            {
                publicKey = publicKey.Substring(2, publicKey.Length - 2);
            }
            publicKey = publicKey.Substring(2, publicKey.Length - 2);
            publicKey = GetKeccak256(publicKey);
            var address = "0x" + publicKey.Substring(publicKey.Length - 40, 40);
            return address;
        }
        public Signature Sign(string hexMsg, byte[] privateKey)
        {
            var msgHashBytes = ByteStringHelper.FromHexString(GetKeccak256(hexMsg));
            var recoverableInfo = CryptoHelper.SignWithPrivateKey(privateKey, msgHashBytes.ToByteArray());
            var rBytes = recoverableInfo.Take(32).ToArray();
            var sBytes = recoverableInfo.Skip(32).Take(32).ToArray();
            var vBytes = recoverableInfo.Skip(64).Take(1).ToArray();
            return new Signature
            {
                HashMsg = msgHashBytes.ToHex(),
                RecoverInfo = recoverableInfo.ToHex(),
                R = rBytes.ToHex(),
                S = sBytes.ToHex(),
                V = vBytes.ToHex()
            };
        }

        public static string GetKeccak256(string hexMsg)
        {
            var offset = hexMsg.StartsWith("0x") ? 2 : 0;

            var txByte = Enumerable.Range(offset, hexMsg.Length - offset)
                .Where(x => x % 2 == 0)
                .Select(x => Convert.ToByte(hexMsg.Substring(x, 2), 16))
                .ToArray();

            //Note: Not intended for intensive use so we create a new Digest.
            //if digest reuse, prevent concurrent access + call Reset before BlockUpdate
            var digest = new KeccakDigest(256);

            digest.BlockUpdate(txByte, 0, txByte.Length);
            var calculatedHash = new byte[digest.GetByteLength()];
            digest.DoFinal(calculatedHash, 0);

            var transactionHash = BitConverter.ToString(calculatedHash, 0, 32).Replace("-", "").ToLower();

            return transactionHash;
        }
    }
}