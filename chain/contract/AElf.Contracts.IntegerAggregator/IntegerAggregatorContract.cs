using System.Globalization;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Standards.ACS13;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.IntegerAggregator
{
    public class IntegerAggregatorContract : IntegerAggregatorContractContainer.IntegerAggregatorContractBase
    {
        public override BytesValue Aggregate(AggregateInput input)
        {
            var actualResults = input.Results.Select(r =>
            {
                var str = StringValue.Parser.ParseFrom(r).Value;
                return str.Contains(';') ? str.Split(';').Last() : str;
            });
            var results = actualResults.Select(decimal.Parse).ToList();
            // Just ignore frequencies, for testing.
            var result = (results.Sum() / results.Count).ToString(CultureInfo.InvariantCulture);
            return new StringValue
            {
                Value = result
            }.ToBytesValue();
        }
    }
}