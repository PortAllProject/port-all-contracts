using System.Collections.Generic;
using System.Threading;
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

            Thread.Sleep(5000);
            for (var i = 0; i < 10; i++)
            {
                var user = UserStubs[i];
                await TokenContractStub.Transfer.SendAsync(new TransferInput
                {
                    To = Users[i].Address,
                    Amount = 30000_00000000,
                    Symbol = "ELF"
                });
                await UserTokenContractStubs[i].Approve.SendAsync(new ApproveInput
                {
                    Spender = DAppContractAddress,
                    Amount = long.MaxValue,
                    Symbol = "ELF"
                });
                await user.Stake.SendAsync(new Int64Value { Value = 100_00000000 });
            }

            await Admin.Draw.SendAsync(new DrawInput { PeriodId = 1 });

            {
                var periodAward = await Admin.GetPeriodAward.CallAsync(new Int64Value
                {
                    Value = 1
                });
                periodAward.StartAwardId.ShouldBe(1);
                periodAward.EndAwardId.ShouldBe(11);
            }

            {
                var awardList = await Admin.GetAwardList.CallAsync(new GetAwardListInput
                {
                    PeriodId = 1
                });
                awardList.Value.Count.ShouldBe(10);
            }
        }

        private async Task InitializeLotteryContract()
        {
            await Admin.Initialize.SendAsync(new InitializeInput
            {
                StartTimestamp = TimestampHelper.GetUtcNow().AddMilliseconds(100),
                ShutdownTimestamp = TimestampHelper.GetUtcNow().AddDays(1),
                RedeemTimestamp = TimestampHelper.GetUtcNow().AddDays(1),
                DefaultAwardList = { GetDefaultAwardList() }
            });
        }

        private List<long> GetDefaultAwardList()
        {
            var awardList = new List<long>
            {
                5000,
                1000, 1000,
                500, 500
            };

            for (var i = 0; i < 5; i++)
            {
                awardList.Add(100);
            }

            for (var i = 0; i < 10; i++)
            {
                awardList.Add(50);
            }

            return awardList;
        }

    }
}