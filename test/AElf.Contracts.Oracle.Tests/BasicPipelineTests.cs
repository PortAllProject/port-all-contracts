using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.OracleUser;
using AElf.ContractTestKit;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContractTests
    {
        private readonly List<Account> _oracleNodeAccounts = SampleAccount.Accounts.Skip(5).Take(5).ToList();

        private List<OracleContractContainer.OracleContractStub> OracleNodeList =>
            _oracleNodeAccounts.Select(a => GetOracleContractStub(a.KeyPair)).ToList();

        private List<Address> OracleNodeAddresses => _oracleNodeAccounts.Select(a => a.Address).ToList();

        private async Task<QueryRecord> QueryTest()
        {
            await InitializeOracleContractAsync();
            await ChangeTokenIssuerToDefaultSenderAsync();

            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                To = OracleUserContractAddress,
                Amount = 100_00000000,
                Symbol = TokenSymbol
            });

            var queryTemperatureInput = new QueryTemperatureInput
            {
                OracleContractAddress = DAppContractAddress,
                AggregatorContractAddress = IntegerAggregatorContractAddress,
                DesignatedNodes = {OracleNodeAddresses},
                AggregateThreshold = 3
            };
            var executionResult = await OracleUserContractStub.QueryTemperature.SendAsync(queryTemperatureInput);
            var queryId = executionResult.Output;

            var queryRecord = await OracleContractStub.GetQueryRecord.CallAsync(queryId);
            queryRecord.DesignatedNodeList.Value.Count.ShouldBe(5);

            return queryRecord;
        }

        private async Task<QueryRecord> CommitTest()
        {
            var queryRecord = await QueryTest();

            await CommitTemperaturesAsync(queryRecord.QueryId, new List<string>
            {
                "10.1",
                "10.2",
                "10.2",
                "10.4"
            });

            var newQueryRecord = await OracleContractStub.GetQueryRecord.CallAsync(queryRecord.QueryId);
            newQueryRecord.IsSufficientCommitmentsCollected.ShouldBeTrue();

            return newQueryRecord;
        }

        [Fact]
        internal async Task RevealTest()
        {
            var queryRecord = await CommitTest();

            await RevealTemperaturesAsync(queryRecord.QueryId, new List<string>
            {
                "10.1",
                "10.2"
            });

            var newQueryRecord = await OracleContractStub.GetQueryRecord.CallAsync(queryRecord.QueryId);
            newQueryRecord.IsSufficientDataCollected.ShouldBeFalse();

            await RevealTemperaturesAsync(queryRecord.QueryId, new List<string>
            {
                "10.1",
                "10.2",
                "10.2"
            }, 2);

            newQueryRecord = await OracleContractStub.GetQueryRecord.CallAsync(queryRecord.QueryId);
            newQueryRecord.IsSufficientDataCollected.ShouldBeTrue();
            newQueryRecord.FinalResult.ShouldBe("10.2");

            await OracleNodeList[3].Reveal.SendWithExceptionAsync(new RevealInput
            {
                QueryId = queryRecord.QueryId,
                Data = "10.4",
                Salt = HashHelper.ComputeFrom("Salt3")
            });
        }

        private async Task CommitTemperaturesAsync(Hash queryId, IReadOnlyList<string> temperatures)
        {
            temperatures = temperatures.Select(t => $"\"{t}\"").ToList();
            for (var i = 0; i < temperatures.Count; i++)
            {
                var temperature = temperatures[i];
                var address = _oracleNodeAccounts[i].Address;
                await OracleNodeList[i].Commit.SendAsync(new CommitInput
                {
                    QueryId = queryId,
                    Commitment = HashHelper.ConcatAndCompute(
                        HashHelper.ComputeFrom(temperature),
                        HashHelper.ConcatAndCompute(HashHelper.ComputeFrom($"Salt{i}"),
                            HashHelper.ComputeFrom(address.ToBase58())))
                });

                var commitmentMap = await OracleContractStub.GetCommitmentMap.CallAsync(queryId);
                commitmentMap.Value.Count.ShouldBe(i + 1);
            }
        }

        private async Task RevealTemperaturesAsync(Hash queryId, List<string> temperatures, int startIndex = 0)
        {
            temperatures = temperatures.Select(t => $"\"{t}\"").ToList();
            for (var i = startIndex; i < temperatures.Count; i++)
            {
                var temperature = temperatures[i];
                await OracleNodeList[i].Reveal.SendAsync(new RevealInput
                {
                    QueryId = queryId,
                    Data = temperature,
                    Salt = HashHelper.ComputeFrom($"Salt{i}")
                });
            }
        }

        [Fact]
        public void ParseJsonTest()
        {
            var result = ParseJson("{\"data\":{\"priceUsd\":\"59207.0439511409731526\"},\"timestamp\":1620463326939}",
                new List<string>
                {
                    "data/priceUsd", "timestamp"
                });

            result.ShouldBe("\"59207.0439511409731526\";1620463326939");
        }

        private string ParseJson(string response, List<string> attributes)
        {
            var jsonDoc = JsonDocument.Parse(response);
            var data = string.Empty;

            foreach (var attribute in attributes)
            {
                if (!attribute.Contains('/'))
                {
                    if (jsonDoc.RootElement.TryGetProperty(attribute, out var targetElement))
                    {
                        if (data == string.Empty)
                        {
                            data = targetElement.GetRawText();
                        }
                        else
                        {
                            data += $";{targetElement.GetRawText()}";
                        }
                    }
                    else
                    {
                        return data;
                    }
                }
                else
                {
                    var attrs = attribute.Split('/');
                    var targetElement = jsonDoc.RootElement.GetProperty(attrs[0]);
                    foreach (var attr in attrs.Skip(1))
                    {
                        if (!targetElement.TryGetProperty(attr, out targetElement))
                        {
                            return attr;
                        }
                    }

                    if (data == string.Empty)
                    {
                        data = targetElement.GetRawText();
                    }
                    else
                    {
                        data += $";{targetElement.GetRawText()}";
                    }
                }
            }

            return data;
        }

        [Fact]
        public void ParseMultipleQueryInfoTest()
        {
            const string coinCapUrl = "https://api.coincap.io/v2/asset/ethereum";
            const string coinGeckoUrl = "https://api.coingecko.com/api/v3/simple/price?ids=ethereum&vs_currencies=usd";
            const string coinGecko2Url = "https://api.coingecko.com/api/v3/simple/price?ids=ethereum&vs_currencies=usd";
            var url = $"{coinCapUrl}|{coinGeckoUrl}|{coinGecko2Url}";
            var attributes = new List<string>
            {
                "data/price|ethereum/usd|ethereum/usd"
            };
            var urls = url.Split('|').ToList();
            var urlAttributes = attributes.Select(a => a.Split('|')).ToList();

            urls.Count.ShouldBe(3);
            urls[0].ShouldBe(coinCapUrl);

            urlAttributes.Select(a => a[0]).ToList()[0].ShouldBe("data/price");
        }

    }
}