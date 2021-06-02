using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS13;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.NumericAggregator
{
    public class NumericAggregatorContract : NumericAggregatorContractContainer.NumericAggregatorContractBase
    {
        public override StringValue Aggregate(AggregateInput input)
        {
            string result;
            var actualResults = GetNumericKeys(input);

            if (input.AggregateOption == 1)
            {
                var indexOfMax = input.Frequencies.IndexOf(input.Frequencies.Max());
                result = actualResults[indexOfMax];
            }
            else
            {
                if (input.AggregateOption == 2)
                {
                    result = GetMiddle(actualResults);
                }
                else
                {
                    for (var index = 0; index < input.Frequencies.Count; index++)
                    {
                        var frequency = input.Frequencies[index];
                        if (frequency > 1)
                        {
                            var needToDup = actualResults[index];
                            for (var i = 0; i < frequency.Sub(1); i++)
                            {
                                actualResults.Add(needToDup);
                            }
                        }
                    }

                    result = GetAverage(actualResults);
                }
            }
            
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

        private List<string> GetNumericKeys(AggregateInput input)
        {
            return input.Results.Select(str =>
            {
                if (str.Contains("\""))
                {
                    str = str.Replace("\"", "");
                }

                return str.Contains(';') ? str.Split(';').Last() : str;
            }).ToList();
        }

        private string GetAverage(IEnumerable<string> actualResults)
        {
            var results = actualResults.Select(decimal.Parse).ToList();
            return (results.Sum() / results.Count).ToString(CultureInfo.InvariantCulture);
        }

        private string GetMiddle(List<string> actualResults)
        {
            return actualResults.OrderBy(p => p).ToList()[actualResults.Count / 2]
                .ToString(CultureInfo.InvariantCulture);
        }
    }
}