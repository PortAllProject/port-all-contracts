using System.Linq;
using AElf.Standards.ACS13;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.StringAggregator
{
    public class StringAggregatorContract : StringAggregatorContractContainer.StringAggregatorContractBase
    {
        public override StringValue Aggregate(AggregateInput input)
        {
            Assert(State.IsEnabled.Value,"The feature is currently disabled.");
            var indexOfMax = input.Frequencies.IndexOf(input.Frequencies.Max());

            return new StringValue
            {
                Value = input.Results[indexOfMax]
            };
        }
    }
}