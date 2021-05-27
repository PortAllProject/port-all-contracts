using System.Collections.Generic;
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

            string result;

            switch (input.AggregateOption)
            {
                case 0:
                    result = GetAverage(actualResults);
                    break;
                case 1:
                    result = GetMajority(actualResults);
                    break;
                case 2:
                    result = GetMiddle(actualResults);
                    break;
                default:
                    result = "Invalid aggregate option.";
                    break;
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

        private string GetAverage(IEnumerable<string> actualResults)
        {
            var results = actualResults.Select(decimal.Parse).ToList();
            return (results.Sum() / results.Count).ToString(CultureInfo.InvariantCulture);
        }

        private string GetMajority(IEnumerable<string> actualResults)
        {
            var countDict = new Dictionary<string, int>();
            foreach (var result in actualResults)
            {
                if (countDict.ContainsKey(result))
                {
                    countDict[result] += 1;
                }
                else
                {
                    countDict.Add(result, 1);
                }
            }

            return countDict.OrderByDescending(d => d.Value).Select(d => d.Key).ToList().First();
        }

        private string GetMiddle(List<string> actualResults)
        {
            return actualResults.OrderBy(p => p).ToList()[actualResults.Count / 2]
                .ToString(CultureInfo.InvariantCulture);
        }
    }
}