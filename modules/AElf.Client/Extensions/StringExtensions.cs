namespace AElf.Client.Extensions;

public static class StringExtensions
{
    public static Address ToAddress(this string? address)
    {
        if (address == null)
        {
            return Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(AElfClientConstants.DefaultPrivateKey));
        }

        return Address.FromBase58(address);
    }
}