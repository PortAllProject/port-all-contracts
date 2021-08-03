using AElf.Database;
using Volo.Abp.Data;

namespace AElf.TokenSwap.Infrastructure
{
    [ConnectionStringName("Default")]
    public class TokenSwapKeyValueDbContext : KeyValueDbContext<TokenSwapKeyValueDbContext>
    {
        
    }
}