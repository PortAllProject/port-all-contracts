using AElf.Cryptography.ECDSA;

namespace AElf.Boilerplate.EventHandler
{
    public interface IKeyStore
    {
        ECKeyPair GetAccountKeyPair();
    }
}