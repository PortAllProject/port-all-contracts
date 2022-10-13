using Google.Protobuf;

namespace AElf.Client.Core;

public class CommonProfile : AutoMapper.Profile
{
    public CommonProfile()
    {
        CreateMap<Hash, string>()
            .ConvertUsing(s => s == null ? null : s.ToHex());

        CreateMap<string, Hash>()
            .ConvertUsing(s => Hash.LoadFromHex(s));

        CreateMap<Address, string>()
            .ConvertUsing(s => s.ToBase58());

        CreateMap<string, Address>()
            .ConvertUsing(s => Address.FromBase58(s));

        CreateMap<ByteString, string>()
            .ConvertUsing(s => s.ToBase64());

        CreateMap<string, ByteString>()
            .ConvertUsing(s => ByteString.CopyFromUtf8(s));
    }
}