using AElf.Kernel.Infrastructure;
using AElf.Types;

namespace AElf.Boilerplate.TestBase
{
    public class MerkleTreeGeneratorSmartContractAddressNameProvider
    {
        public static readonly Hash Name = HashHelper.ComputeFrom("AElf.ContractNames.MerkleTreeGenerator");

        public static readonly string StringName = Name.ToStorageKey();
        public Hash ContractName => Name;
        public string ContractStringName => StringName;
    }
}