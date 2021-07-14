using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Contracts.Bridge;
using AElf.Standards.ACS13;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContractTests
    {
        [Fact]
        public async Task ConverterTest()
        {
            var aggregateInput = new AggregateInput
            {
                Results =
                {
                    "1.111", "2.222", "3.333"
                },
                Frequencies = {1, 1, 1}
            };
            var result = await IntegerAggregatorContractStub.Aggregate.CallAsync(aggregateInput);
            result.Value.ShouldBe("2.222");
        }

        [Fact]
        public void TypeTest()
        {
            var byteString = ByteString.CopyFrom("eantest".GetBytes());
            byteString.Length.ShouldBe(7);
            byteString.ToByteArray().Length.ShouldBe(7);
            Encoding.UTF8.GetString(byteString.ToByteArray()).ShouldBe("eantest");

            const string test = "testtfdsafdsafdsafsa";
            var bytes = test.GetBytes();
            byteString = ByteString.CopyFrom(bytes);
            byteString.ToByteArray().ShouldBe(bytes);
        }

        [Fact]
        public void MajorityResultTest()
        {
            GetMajorityResult(new PlainResult
            {
                DataRecords = new DataRecords
                {
                    Value =
                    {
                        new DataRecord {Data = "10"},
                        new DataRecord {Data = "1"},
                        new DataRecord {Data = "11"},
                        new DataRecord {Data = "13"},
                        new DataRecord {Data = "14"},
                        new DataRecord {Data = "15"},
                    }
                }
            }).ShouldBe("10");
        }

        private string GetMajorityResult(PlainResult plainResult)
        {
            var results = plainResult.DataRecords.Value.Select(r => r.Data);
            var countDict = new Dictionary<string, int>();
            foreach (var result in results)
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


    }
}