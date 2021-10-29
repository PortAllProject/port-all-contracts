namespace AElf.EventHandler
{
    public class ContractAbiOptions
    {
        public string TransmitAbiFilePath { get; set; }
        public string LockAbiFilePath { get; set; } = "./ContractBuild/LockAbi.json";
        public string LockWithTakeTokenAbiFilePath { get; set; } = "./ContractBuild/LockWithTakeTokenAbi.json";
    }
}