using AElf.TokenSwap.Infrastructure;
using Google.Protobuf;

namespace AElf.TokenSwap
{
    public interface ITokenSwapStore<T> : IKeyValueStore<T>
        where T : class, IMessage<T>, new()
    {
    }

    public class TokenSwapStore<T> : KeyValueStoreBase<TokenSwapKeyValueDbContext, T>, ITokenSwapStore<T>
        where T : class, IMessage<T>, new()
    {
        public TokenSwapStore(TokenSwapKeyValueDbContext keyValueDbContext, IStoreKeyPrefixProvider<T> prefixProvider)
            : base(keyValueDbContext, prefixProvider)
        {
        }
    }
}