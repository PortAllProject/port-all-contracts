using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Lottery.Tests
{
    public class LotteryContractTests : LotteryContractTestBase
    {
        [Fact]
        public async Task DrawTest()
        {
            await Admin.Initialize.SendAsync(new InitializeInput
            {
                StartTimestamp = TimestampHelper.GetUtcNow().AddMilliseconds(100),
                ShutdownTimestamp = TimestampHelper.GetUtcNow().AddMilliseconds(10000),
                RedeemTimestamp = TimestampHelper.GetUtcNow().AddMilliseconds(10000),
                IsDebug = true
            });

            for (var i = 0; i < 20; i++)
            {
                await TokenContractStub.Transfer.SendAsync(new TransferInput
                {
                    To = Users[i].Address,
                    Amount = 30_0000_00000000,
                    Symbol = "ELF"
                });
                await UserTokenContractStubs[i].Approve.SendAsync(new ApproveInput
                {
                    Spender = DAppContractAddress,
                    Amount = long.MaxValue,
                    Symbol = "ELF"
                });
            }

            for (var i = 0; i < 6; i++)
            {
                var user = UserStubs[i];
                await user.Stake.SendAsync(new Int64Value {Value = 19100_00000000});
            }

            {
                var totalLotteryCodeCount = (await Admin.GetTotalLotteryCount.CallAsync(new Empty())).Value;
                var awardList = (await Admin.GetAwardList.CallAsync(new GetAwardListInput
                {
                    PeriodId = 1
                }));
            }

            await Admin.Draw.SendAsync(new DrawInput {PeriodId = 1, ToAwardId = 40});
            await Admin.Draw.SendAsync(new DrawInput {PeriodId = 1, ToAwardId = 70});
            await Admin.Draw.SendAsync(new DrawInput {PeriodId = 1});

            {
                var awardList = (await Admin.GetAwardList.CallAsync(new GetAwardListInput
                {
                    PeriodId = 1
                }));
                awardList.Value.Count.ShouldBe(120);
            }
        }

        [Fact]
        public async Task PipelineTest()
        {
            await InitializeLotteryContract();

            {
                var periodAward = await Admin.GetPeriodAward.CallAsync(new Int64Value
                {
                    Value = 1
                });
                periodAward.StartAwardId.ShouldBe(1);
                periodAward.EndAwardId.ShouldBe(20);
            }

            for (var i = 0; i < 10; i++)
            {
                var user = UserStubs[i];
                await user.Stake.SendAsync(new Int64Value {Value = 100_00000000});
            }

            await Admin.Draw.SendAsync(new DrawInput {PeriodId = 1});

            {
                var periodAward = await Admin.GetPeriodAward.CallAsync(new Int64Value
                {
                    Value = 1
                });
                periodAward.StartAwardId.ShouldBe(1);
                periodAward.EndAwardId.ShouldBe(10);
            }

            {
                var awardList = await Admin.GetAwardList.CallAsync(new GetAwardListInput
                {
                    PeriodId = 1
                });
                awardList.Value.Count.ShouldBe(10);
            }

            for (var i = 0; i < 10; i++)
            {
                var user = UserStubs[i];
                await user.Stake.SendAsync(new Int64Value {Value = 1000_00000000});
            }

            await Admin.Draw.SendAsync(new DrawInput {PeriodId = 2});

            {
                var periodAward = await Admin.GetPeriodAward.CallAsync(new Int64Value
                {
                    Value = 2
                });
                periodAward.StartAwardId.ShouldBe(11);
                periodAward.EndAwardId.ShouldBe(30);
            }

            {
                var awardList = await Admin.GetAwardList.CallAsync(new GetAwardListInput
                {
                    PeriodId = 2
                });
                awardList.Value.Count.ShouldBe(20);
            }

            for (var i = 0; i < 10; i++)
            {
                var user = UserStubs[i];
                await user.Claim.SendAsync(new Empty());
            }

            await UserStubs[10].Stake.SendAsync(new Int64Value
            {
                Value = 19100_00000000
            });
            {
                var ownLottery = await Admin.GetOwnLottery.CallAsync(Users[10].Address);
                ownLottery.LotteryCodeList.Count.ShouldBe(20);
            }
        }

        [Theory]
        [InlineData(99, 0)]
        [InlineData(100, 1)]
        [InlineData(999, 1)]
        [InlineData(1099, 1)]
        [InlineData(1100, 2)]
        [InlineData(19100, 20)]
        [InlineData(20100, 21)]
        [InlineData(99999, 21)]
        public async Task StakeAndGetCorrectLotteryCodeCountTest(long stakingAmount, int lotteryCodeCount)
        {
            await InitializeLotteryContract();
            var user = Users.First();
            var userStub = UserStubs.First();
            await userStub.Stake.SendAsync(new Int64Value
            {
                Value = stakingAmount * 1_00000000
            });
            var ownLottery = await userStub.GetOwnLottery.CallAsync(user.Address);
            ownLottery.LotteryCodeList.Count.ShouldBe(lotteryCodeCount);
        }

        private async Task InitializeLotteryContract()
        {
            await Admin.Initialize.SendAsync(new InitializeInput
            {
                StartTimestamp = TimestampHelper.GetUtcNow().AddMilliseconds(100),
                ShutdownTimestamp = TimestampHelper.GetUtcNow().AddMilliseconds(10000),
                RedeemTimestamp = TimestampHelper.GetUtcNow().AddMilliseconds(10000),
                DefaultAwardList = {GetDefaultAwardList()},
                IsDebug = true
            });

            for (var i = 0; i < 20; i++)
            {
                await TokenContractStub.Transfer.SendAsync(new TransferInput
                {
                    To = Users[i].Address,
                    Amount = 30_0000_00000000,
                    Symbol = "ELF"
                });
                await UserTokenContractStubs[i].Approve.SendAsync(new ApproveInput
                {
                    Spender = DAppContractAddress,
                    Amount = long.MaxValue,
                    Symbol = "ELF"
                });
            }
        }

        private List<long> GetDefaultAwardList()
        {
            var awardList = new List<long>
            {
                5000_00000000,
                1000_00000000, 1000_00000000,
                500_00000000, 500_00000000
            };

            for (var i = 0; i < 5; i++)
            {
                awardList.Add(100_00000000);
            }

            for (var i = 0; i < 10; i++)
            {
                awardList.Add(50_00000000);
            }

            return awardList;
        }

    }
}