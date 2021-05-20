using System.Linq;
using AElf.Standards.ACS13;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.CrossChainToken
{
    public partial class CrossChainTokenContract
    {
        public override StringValue Aggregate(AggregateInput input)
        {
            var indexOfMax = input.Frequencies.IndexOf(input.Frequencies.Max());
            return new StringValue {Value = input.Results[indexOfMax]};
        }
    }
}