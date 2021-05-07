using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AElf.Boilerplate.Ethereum
{
    [Event("RoundRequested")]
    public class RoundRequestedEventDto : IEventDTO
    {
        [Parameter("address", "requester", 1, true)]
        public string Requester { get; set; }

        [Parameter("bytes16", "configDigest", 2, false)]
        public string ConfigDigest { get; set; }

        [Parameter("uint64", "roundId", 3, false)]
        public BigInteger RoundId { get; set; }
    }
}