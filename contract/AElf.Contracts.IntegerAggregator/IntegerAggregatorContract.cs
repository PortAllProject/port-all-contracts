using System.Globalization;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS13;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.IntegerAggregator
{
    public class IntegerAggregatorContract : IntegerAggregatorContractContainer.IntegerAggregatorContractBase
    {
        public override StringValue Aggregate(AggregateInput input)
        {
            var actualResults = input.Results.Select(str =>
            {
                if (str.Contains("\""))
                {
                    str = str.Replace("\"", "");
                }

                return str.Contains(';') ? str.Split(';').Last() : str;
            }).ToList();
            var results = actualResults.Select(decimal.Parse).ToList();
            // Just ignore frequencies, for testing.
            var result = (results.Sum() / results.Count).ToString(CultureInfo.InvariantCulture);

            Context.Fire(new AggregateDataReceived
            {
                Results = new Results {Value = {actualResults}},
                FinalResult = result
            });
            return new StringValue
            {
                Value = result
            };
        }
    }
}