using AElf.Client.Core;
using Volo.Abp.Modularity;

namespace AElf.Client.Report;

[DependsOn(
    typeof(AElfClientModule),
    typeof(CoreAElfModule)
)]
public class AElfClientReportModule : AbpModule
{
}