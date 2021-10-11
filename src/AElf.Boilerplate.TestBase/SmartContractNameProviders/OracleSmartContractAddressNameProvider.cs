using AElf.Kernel.Infrastructure;
using AElf.Types;

namespace AElf.Boilerplate.TestBase
{
    public class OracleSmartContractAddressNameProvider
    {
        public static readonly Hash Name = HashHelper.ComputeFrom("AElf.ContractNames.Oracle");

        public static readonly string StringName = Name.ToStorageKey();
        public Hash ContractName => Name;
        public string ContractStringName => StringName;
    }
}