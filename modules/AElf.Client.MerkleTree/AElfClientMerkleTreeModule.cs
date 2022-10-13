using AElf.Client.Core;
using Volo.Abp.Modularity;

namespace AElf.Client.MerkleTree;

[DependsOn(
    typeof(AElfClientModule),
    typeof(CoreAElfModule)
)]
public class AElfClientMerkleTreeModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        base.ConfigureServices(context);
    }
}