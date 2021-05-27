using AElf.Cryptography.ECDSA;

namespace AElf.EventHandler
{
    public interface IKeyStore
    {
        ECKeyPair GetAccountKeyPair();
    }
}