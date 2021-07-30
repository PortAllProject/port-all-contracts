using Volo.Abp;
using Volo.Abp.Testing;

namespace AElf.EventHandler.Tests
{
    public class AElfEventHandlerTestBase : AbpIntegratedTest<AElfEventHandlerTestModule>
    {
        protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
        {
            options.UseAutofac();
        }
    }
}