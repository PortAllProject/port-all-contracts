using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Regiment;
using AElf.Contracts.Report;
using AElf.ContractTestKit;
using AElf.CSharp.Core.Extension;
using AElf.Types;
using Shouldly;
using Xunit;
using Account = AElf.ContractTestBase.ContractTestKit.Account;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContractTests
    {
        private List<Account> ObserverAccounts => Accounts.Skip(10).Take(5).ToList();
        private List<Address> ObserverAddresses => ObserverAccounts.Select(a => a.Address).ToList();

        private List<TokenContractContainer.TokenContractStub> ObserverTokenStubs => ObserverAccounts
            .Select(a => GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, a.KeyPair))
            .ToList();

        private List<OracleContractContainer.OracleContractStub> ObserverOracleStubs => ObserverAccounts
            .Select(a => GetTester<OracleContractContainer.OracleContractStub>(DAppContractAddress, a.KeyPair))
            .ToList();

        private List<ReportContractContainer.ReportContractStub> ObserverStubs => ObserverAccounts
            .Select(a => GetTester<ReportContractContainer.ReportContractStub>(ReportContractAddress, a.KeyPair))
            .ToList();
        
        private List<RegimentContractContainer.RegimentContractStub> ObserverRegimentStubs => ObserverAccounts
            .Select(a => GetTester<RegimentContractContainer.RegimentContractStub>(RegimentContractAddress, a.KeyPair))
            .ToList();

        private Address _regimentAddress;

        private async Task CreateRegiment()
        {
            var executionResult = await ObserverOracleStubs[0].CreateRegiment.SendAsync(new CreateRegimentInput
            {
                InitialMemberList = {ObserverAddresses}
            });
            var logEvent = executionResult.TransactionResult.Logs.Single(l => l.Name == nameof(RegimentCreated));
            var regimentCreated = new RegimentCreated();
            regimentCreated.MergeFrom(logEvent);
            _regimentAddress = regimentCreated.RegimentAddress;
        }

        [Fact]
        internal async Task<OffChainAggregationInfo> AddOffChainAggregationInfoTest()
        {
            await InitializeOracleContractAsync();
            await ChangeTokenIssuerToDefaultSenderAsync();
            await InitializeReportContractAsync();
            await CreateRegiment();
            await ApplyObserversAsync();
            var digestStr = "0xf6f3ed664fd0e7be332f035ec351acf1";
            var registerOffChainAggregationInput = new RegisterOffChainAggregationInput
            {
                RegimentAddress = _regimentAddress,
                OffChainQueryInfoList = new OffChainQueryInfoList
                {
                    Value =
                    {
                        new OffChainQueryInfo
                        {
                            Title = "www.whatever.com",
                            Options = {"foo"}
                        }
                    }
                },
                Token = Token,
                ConfigDigest = ByteStringHelper.FromHexString(digestStr),
                AggregateThreshold = 5,
                AggregatorContractAddress = IntegerAggregatorContractAddress
            };
            var executionResult =
                await ReportContractStub.RegisterOffChainAggregation.SendAsync(registerOffChainAggregationInput);

            var offChainAggregationInfo = executionResult.Output;
            offChainAggregationInfo.OffChainQueryInfoList.Value[0].Title.ShouldBe(registerOffChainAggregationInput
                .OffChainQueryInfoList.Value[0]
                .Title);
            offChainAggregationInfo.OffChainQueryInfoList.Value[0].Options[0].ShouldBe(
                registerOffChainAggregationInput
                    .OffChainQueryInfoList.Value[0].Options[0]);
            offChainAggregationInfo.Token.ShouldBe(registerOffChainAggregationInput.Token);
            offChainAggregationInfo.ConfigDigest.ToHex()
                .ShouldBe(registerOffChainAggregationInput.ConfigDigest.ToHex());
            offChainAggregationInfo.AggregateThreshold.ShouldBe(registerOffChainAggregationInput.AggregateThreshold);

            return offChainAggregationInfo;
        }

        [Fact]
        internal async Task<QueryRecord> QueryOracleTest()
        {
            var offChainAggregationInfo = await AddOffChainAggregationInfoTest();

            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = TokenSymbol,
                Amount = 1000_00000000,
                To = DefaultSender
            });
            await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = ReportContractAddress,
                Symbol = TokenSymbol,
                Amount = long.MaxValue
            });
            var queryOracleInput = new QueryOracleInput
            {
                Payment = 10_00000000,
                Token = Token
            };
            var executionResult = await ReportContractStub.QueryOracle.SendAsync(queryOracleInput);
            var queryId = executionResult.Output;

            // Assert query created successfully.
            var queryRecord = await OracleContractStub.GetQueryRecord.CallAsync(queryId);
            queryRecord.Payment.ShouldBe(queryOracleInput.Payment);
            queryRecord.DesignatedNodeList.Value.First()
                .ShouldBe(offChainAggregationInfo.RegimentAddress);

            var reportQueryRecord = await ReportContractStub.GetReportQueryRecord.CallAsync(queryId);
            reportQueryRecord.OriginQuerySender.ShouldBe(DefaultSender);
            reportQueryRecord.PaidReportFee.ShouldBe(DefaultReportFee);

            return queryRecord;
        }

        [Fact]
        internal async Task ReportTest()
        {
            var queryRecord = await QueryOracleTest();
            await OracleContractStub.GetQueryRecord.CallAsync(queryRecord.QueryId);
            await CommitAsync(queryRecord.QueryId);
            await RevealAsync(queryRecord.QueryId);
            var report = await ReportContractStub.GetReport.CallAsync(new GetReportInput
            {
                Token = Token,
                RoundId = 1
            });
            for (var i = 0; i < report.Observations.Value.Count; i++)
            {
                report.Observations.Value[i].Data
                    .ShouldBe(((decimal) (i * 1.111)).ToString(CultureInfo.InvariantCulture));
            }

            report.AggregatedData.ToStringUtf8().ShouldBe("2.222");
        }

        [Fact]
        internal async Task MerkleAggregationTest()
        {
            await InitializeOracleContractAsync();
            await ChangeTokenIssuerToDefaultSenderAsync();
            await InitializeReportContractAsync();
            await CreateRegiment();
            await ApplyObserversAsync();
            var digestStr = "0xf6f3ed664fd0e7be332f035ec351acf1";
            var addOffChainAggregatorInput = new RegisterOffChainAggregationInput
            {
                RegimentAddress = _regimentAddress,
                OffChainQueryInfoList = new OffChainQueryInfoList
                {
                    Value =
                    {
                        new OffChainQueryInfo
                        {
                            Title = "www.whatever.com",
                            Options = {"foo"}
                        },
                        new OffChainQueryInfo
                        {
                            Title = "www.youbiteme.com",
                            Options = {"bar"}
                        },
                        new OffChainQueryInfo
                        {
                            Title = "www.helloworld.com",
                            Options = {"yes"}
                        },
                    }
                },
                Token = Token,
                ConfigDigest = ByteStringHelper.FromHexString(digestStr),
                AggregateThreshold = 5,
                AggregatorContractAddress = IntegerAggregatorContractAddress
            };
            var offChainAggregationInfo =
                (await ReportContractStub.RegisterOffChainAggregation.SendAsync(addOffChainAggregatorInput)).Output;
            offChainAggregationInfo.OffChainQueryInfoList.Value[2].Title.ShouldBe(addOffChainAggregatorInput
                .OffChainQueryInfoList.Value[2]
                .Title);
            offChainAggregationInfo.OffChainQueryInfoList.Value[2].Options[0].ShouldBe(
                addOffChainAggregatorInput
                    .OffChainQueryInfoList.Value[2].Options[0]);

            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = TokenSymbol,
                Amount = 1000_00000000,
                To = DefaultSender
            });
            await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = ReportContractAddress,
                Symbol = TokenSymbol,
                Amount = long.MaxValue
            });

            var queryId1 = (await ReportContractStub.QueryOracle.SendAsync(new QueryOracleInput
            {
                Payment = 10_00000000,
                Token = Token
            })).Output;
            await CommitAsync(queryId1);
            await RevealAsync(queryId1);

            var queryId2 = (await ReportContractStub.QueryOracle.SendAsync(new QueryOracleInput
            {
                Payment = 10_00000000,
                Token = Token,
                NodeIndex = 1
            })).Output;
            await CommitAsync(queryId2);
            await RevealAsync(queryId2);

            var queryId3 = (await ReportContractStub.QueryOracle.SendAsync(new QueryOracleInput
            {
                Payment = 10_00000000,
                Token = Token,
                NodeIndex = 2
            })).Output;
            await CommitAsync(queryId3);
            await RevealAsync(queryId3);

            var report = await ReportContractStub.GetReport.CallAsync(new GetReportInput
            {
                Token = offChainAggregationInfo.Token,
                RoundId = 1
            });
            foreach (var observation in report.Observations.Value)
            {
                observation.Data.ShouldBe("2.222");
            }

            var string2Hash = HashHelper.ComputeFrom("2.222");
            var supposedMerkleTree = BinaryMerkleTree.FromLeafNodes(new[] {string2Hash, string2Hash, string2Hash});
            report.AggregatedData.ShouldBe(supposedMerkleTree.Root.Value);
        }

        private async Task CommitAsync(Hash queryId)
        {
            for (var i = 0; i < ObserverOracleStubs.Count; i++)
            {
                var address = ObserverAccounts[i].Address;
                await ObserverOracleStubs[i].Commit.SendAsync(new CommitInput
                {
                    QueryId = queryId,
                    Commitment = HashHelper.ConcatAndCompute(
                        HashHelper.ComputeFrom(((decimal) (i * 1.111)).ToString(CultureInfo.InvariantCulture)),
                        HashHelper.ConcatAndCompute(HashHelper.ComputeFrom($"Salt{i}"),
                            HashHelper.ComputeFrom(address.ToBase58())))
                });

                var commitmentMap = await OracleContractStub.GetCommitmentMap.CallAsync(queryId);
                commitmentMap.Value.Count.ShouldBe(i + 1);
            }
        }

        private async Task RevealAsync(Hash queryId)
        {
            for (var i = 0; i < ObserverOracleStubs.Count; i++)
            {
                await ObserverOracleStubs[i].Reveal.SendAsync(new RevealInput
                {
                    QueryId = queryId,
                    Data = ((decimal) (i * 1.111)).ToString(CultureInfo.InvariantCulture),
                    Salt = HashHelper.ComputeFrom($"Salt{i}")
                });
            }
        }

        private async Task InitializeReportContractAsync()
        {
            var input = new Report.InitializeInput
            {
                ApplyObserverFee = DefaultApplyObserverFee,
                ReportFee = 1_00000000,
                OracleContractAddress = DAppContractAddress,
                RegimentContractAddress = RegimentContractAddress
            };
            input.InitialRegisterWhiteList.Add(SampleAccount.Accounts.First().Address);
            await ReportContractStub.Initialize.SendAsync(input);
        }

        private async Task ApplyObserversAsync()
        {
            foreach (var address in ObserverAddresses)
            {
                await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = TokenSymbol,
                    Amount = DefaultApplyObserverFee * 10,
                    To = address
                });
            }

            foreach (var tokenStub in ObserverTokenStubs)
            {
                await tokenStub.Approve.SendAsync(new ApproveInput
                {
                    Spender = ReportContractAddress,
                    Amount = long.MaxValue,
                    Symbol = TokenSymbol
                });
            }

            foreach (var observerStub in ObserverStubs)
            {
                await observerStub.ApplyObserver.SendAsync(new ApplyObserverInput
                {
                    RegimentAddressList = {_regimentAddress}
                });
            }
        }
    }
}