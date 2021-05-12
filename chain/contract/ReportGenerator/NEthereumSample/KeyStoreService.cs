using Nethereum.KeyStore.Model;

namespace ReportGenerator
{
    public static class KeyStoreService
    {
        public static void Decrypt()
        {
            var keyStoreService = new Nethereum.KeyStore.KeyStoreScryptService();
            var scryptParams = new ScryptParams {Dklen = 32, N = 262144, R = 1, P = 8};
            var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
            var password = "testPassword";
            var keyStore = keyStoreService.EncryptAndGenerateKeyStore(password, ecKey.GetPrivateKeyAsBytes(), ecKey.GetPublicAddress(), scryptParams);
            var json = keyStoreService.SerializeKeyStoreToJson(keyStore);
            var key = keyStoreService.DecryptKeyStoreFromJson(password, json);
        }
        
    }
}