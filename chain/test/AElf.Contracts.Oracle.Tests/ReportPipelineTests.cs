using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Report;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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

        [Fact]
        internal async Task<OffChainAggregatorContractInfo> AddOffChainAggregatorTest()
        {
            await InitializeOracleContractAsync();
            await ChangeTokenIssuerToDefaultSenderAsync();
            await InitializeReportContractAsync();
            await ApplyObserversAsync();
            var addOffChainAggregatorInput = new AddOffChainAggregatorInput
            {
                ObserverList = new ObserverList
                {
                    Value = {ObserverAddresses}
                },
                OffChainInfo =
                {
                    new OffChainInfo
                    {
                        UrlToQuery = "www.whatever.com",
                        AttributeToFetch = "foo"
                    }
                },
                EthereumContractAddress = "1234567890123",
                ConfigDigest = ByteString.CopyFromUtf8("bar"),
                AggregateThreshold = 5,
                AggregatorContractAddress = IntegerAggregatorContractAddress
            };
            var executionResult = await ReportContractStub.AddOffChainAggregator.SendAsync(addOffChainAggregatorInput);

            var offChainAggregatorContractInfo = executionResult.Output;
            offChainAggregatorContractInfo.OffChainInfo[0].UrlToQuery.ShouldBe(addOffChainAggregatorInput
                .OffChainInfo[0]
                .UrlToQuery);
            offChainAggregatorContractInfo.OffChainInfo[0].AttributeToFetch.ShouldBe(addOffChainAggregatorInput
                .OffChainInfo[0].AttributeToFetch);
            offChainAggregatorContractInfo.EthereumContractAddress.ShouldBe(addOffChainAggregatorInput
                .EthereumContractAddress);
            offChainAggregatorContractInfo.ConfigDigest.ToHex()
                .ShouldBe(addOffChainAggregatorInput.ConfigDigest.ToHex());
            offChainAggregatorContractInfo.AggregateThreshold.ShouldBe(addOffChainAggregatorInput.AggregateThreshold);
            // Association created.
            var organization =
                await AssociationContractStub.GetOrganization.CallAsync(offChainAggregatorContractInfo
                    .ObserverAssociationAddress);
            organization.OrganizationMemberList.OrganizationMembers.Count.ShouldBe(5);
            organization.ProposerWhiteList.Proposers.First().ShouldBe(ReportContractAddress);

            return offChainAggregatorContractInfo;
        }

        [Fact]
        internal async Task<QueryRecord> QueryOracleTest()
        {
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

            var offChainAggregatorContractInfo = await AddOffChainAggregatorTest();
            var queryOracleInput = new QueryOracleInput
            {
                Payment = 10_00000000,
                ObserverAssociationAddress = offChainAggregatorContractInfo.ObserverAssociationAddress
            };
            var executionResult = await ReportContractStub.QueryOracle.SendAsync(queryOracleInput);
            var queryId = executionResult.Output;

            // Assert query created successfully.
            var queryRecord = await OracleContractStub.GetQueryRecord.CallAsync(queryId);
            queryRecord.Payment.ShouldBe(queryOracleInput.Payment);
            queryRecord.DesignatedNodeList.Value.First()
                .ShouldBe(offChainAggregatorContractInfo.ObserverAssociationAddress);

            var reportQueryRecord = await ReportContractStub.GetReportQueryRecord.CallAsync(queryId);
            reportQueryRecord.OriginQueryManager.ShouldBe(DefaultSender);
            reportQueryRecord.PaidReportFee.ShouldBe(DefaultReportFee);

            return queryRecord;
        }

        [Fact]
        internal async Task ReportTest()
        {
            var queryRecord = await QueryOracleTest();
            var query = await OracleContractStub.GetQueryRecord.CallAsync(queryRecord.QueryId);
            await CommitAsync(queryRecord.QueryId);
            await RevealAsync(queryRecord.QueryId);
            var report = await ReportContractStub.GetReport.CallAsync(new GetReportInput
            {
                ObserverAssociationAddress = query.DesignatedNodeList.Value.First(),
                RoundId = 1
            });
            for (var i = 0; i < report.Observations.Value.Count; i++)
            {
                report.Observations.Value[i].Data.ShouldBe(new StringValue {Value = i.ToString()}.ToByteString());
            }

            var aggregatedValue = new StringValue();
            aggregatedValue.MergeFrom(report.AggregatedData);
            aggregatedValue.Value.ShouldBe("2");
        }

        [Fact]
        internal async Task MerkleAggregationTest()
        {
            await InitializeOracleContractAsync();
            await ChangeTokenIssuerToDefaultSenderAsync();
            await InitializeReportContractAsync();
            await ApplyObserversAsync();
            var addOffChainAggregatorInput = new AddOffChainAggregatorInput
            {
                ObserverList = new ObserverList
                {
                    Value = {ObserverAddresses}
                },
                OffChainInfo =
                {
                    new OffChainInfo
                    {
                        UrlToQuery = "www.whatever.com",
                        AttributeToFetch = "foo"
                    },
                    new OffChainInfo
                    {
                        UrlToQuery = "www.youbiteme.com",
                        AttributeToFetch = "bar"
                    },
                    new OffChainInfo
                    {
                        UrlToQuery = "www.helloworld.com",
                        AttributeToFetch = "yes"
                    },
                },
                EthereumContractAddress = "1234567890123",
                ConfigDigest = ByteString.CopyFromUtf8("123"),
                AggregateThreshold = 5,
                AggregatorContractAddress = IntegerAggregatorContractAddress
            };
            var offChainAggregatorContractInfo =
                (await ReportContractStub.AddOffChainAggregator.SendAsync(addOffChainAggregatorInput)).Output;
            offChainAggregatorContractInfo.OffChainInfo[2].UrlToQuery.ShouldBe(addOffChainAggregatorInput
                .OffChainInfo[2]
                .UrlToQuery);
            offChainAggregatorContractInfo.OffChainInfo[2].AttributeToFetch.ShouldBe(addOffChainAggregatorInput
                .OffChainInfo[2].AttributeToFetch);

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
                ObserverAssociationAddress = offChainAggregatorContractInfo.ObserverAssociationAddress
            })).Output;
            await CommitAsync(queryId1);
            await RevealAsync(queryId1);

            var queryId2 = (await ReportContractStub.QueryOracle.SendAsync(new QueryOracleInput
            {
                Payment = 10_00000000,
                ObserverAssociationAddress = offChainAggregatorContractInfo.ObserverAssociationAddress,
                NodeIndex = 1
            })).Output;
            await CommitAsync(queryId2);
            await RevealAsync(queryId2);

            var queryId3 = (await ReportContractStub.QueryOracle.SendAsync(new QueryOracleInput
            {
                Payment = 10_00000000,
                ObserverAssociationAddress = offChainAggregatorContractInfo.ObserverAssociationAddress,
                NodeIndex = 2
            })).Output;
            await CommitAsync(queryId3);
            await RevealAsync(queryId3);

            var report = await ReportContractStub.GetReport.CallAsync(new GetReportInput
            {
                ObserverAssociationAddress = offChainAggregatorContractInfo.ObserverAssociationAddress,
                RoundId = 1
            });
            var string2 = new StringValue {Value = "2"};
            foreach (var observation in report.Observations.Value)
            {
                observation.Data.ShouldBe(string2.ToByteString());
            }

            var string2Hash = HashHelper.ComputeFrom(string2.ToByteArray());
            var supposedMerkleTree = BinaryMerkleTree.FromLeafNodes(new[] {string2Hash, string2Hash, string2Hash});
            report.AggregatedData.ShouldBe(supposedMerkleTree.Root.Value);
        }

        private async Task CommitAsync(Hash queryId)
        {
            for (var i = 0; i < ObserverOracleStubs.Count; i++)
            {
                await ObserverOracleStubs[i].Commit.SendAsync(new CommitInput
                {
                    QueryId = queryId,
                    Commitment = HashHelper.ConcatAndCompute(
                        HashHelper.ComputeFrom(new StringValue {Value = i.ToString()}),
                        HashHelper.ComputeFrom($"Salt{i}"))
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
                    Data = new StringValue {Value = i.ToString()}.ToByteString(),
                    Salt = HashHelper.ComputeFrom($"Salt{i}")
                });
            }
        }

        private async Task InitializeReportContractAsync()
        {
            await ReportContractStub.Initialize.SendAsync(new Report.InitializeInput
            {
                ApplyObserverFee = DefaultApplyObserverFee,
                ReportFee = 1_00000000,
                OracleContractAddress = DAppContractAddress
            });
        }

        private async Task ApplyObserversAsync()
        {
            foreach (var address in ObserverAddresses)
            {
                await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = TokenSymbol,
                    Amount = DefaultApplyObserverFee * 2,
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
                await observerStub.ApplyObserver.SendAsync(new ApplyObserverInput());
            }
        }
    }
}