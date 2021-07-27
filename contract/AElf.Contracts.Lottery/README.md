# Lottery Contract
用于aelf主网换币活动后的抽奖活动。

## Initialize
初始化该合约时，至少需要输入：
- start_timestamp，活动开始时间（从这个时间点后，用户可以锁定ELF代币获得抽奖码）
- shutdown_timestamp，活动结束时间（从这个时间点后，用户不再能锁定ELF代币获得抽奖码）
- redeem_timestamp，开始赎回时间（从这个时间点后，用户可以赎回之前锁定的ELF代币）

以上三个时间必须为递增，后两者可以相同。

可选输入Admin地址，不输入的话，Initialize交易的Sender会成为Admin。Admin将有权限调用`Draw`方法进行开奖。

可选输入默认每期的ELF奖项：
- default_award_list，是一个长整型数组

如果不输入的话，使用以下数据：
```C#
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

    for (var i = 0; i < 100; i++)
    {
        awardList.Add(10);
    }

    return awardList;
}
```
即默认为一个长度为120的长整型数组，包括1个5000，2个1000，2个500，5个100，10个50，100个10。

is_debug如果为true，会忽略对活动时间相关的Assertion，正式环境不要设置。

## Stake
用户通过Stake方法锁定代币，其参数为用户本次锁定的代币数额。

Lottery合约会自动为用户兑换出合适数量的抽奖码：第一个价值100ELF，之后的价值10000ELF，相关常数配置在LotteryContract_Rules.cs文件里，可以通过升级合约修改。

|  用户锁定代币总数   | 可拥有抽奖码个数  |
|  ----  | ----  |
| 99  | 0 |
| 100  | 1 |
| 999  | 1 |
| 1100  | 2 |
| 20100  | 21 |
| 99999  | 21 |

用户可以分批多次锁定不同额度的代币，得到的抽奖码个数只与staking的**总数额**相关。

例如第一次锁定1099个ELF，可以获得第一个抽奖码；第二次锁定1个ELF，可以获得第二个抽奖码。

相关测试：
```C#
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
```

## Draw
Lottery合约的Admin通过Draw方法开奖。

DrawInput包含三个参数：
- period_id（必填）：填入要开奖的期的期数，主要为了防止重复开奖；
- next_award_list（选填）：用于修改下一期的奖品，不填的话使用`GetDefaultAwardList()`中的设置；
- to_award_id（选填）：指示本次开奖开到那个award id，不填的话开完本期所有奖项。


开奖结束后：
- 本期所有`Award`的`lottery_code`字段将会被分配上获奖的抽奖码；
- 相关的`Lottery`的`award_id_list`字段也会加入刚获奖奖项的`award_id`。

### 开奖逻辑

当前有两种开奖的逻辑：
- 当抽奖码数量小于或者等于二倍的奖项数量时，会构建一个lottery pool，不断使用随机数对lottery pool count取余，得到中奖的抽奖码的下标，随后将该抽奖码移出lottery pool；此时循环的次数为`math.min(lottery pool count, drew award count)`
- 当抽奖码数量大于二倍的奖项数量时，就用随机数对抽奖码的数量取余，在一次开奖过程中如果有重复中奖的情况就调整随机数（一个抽奖码每一期只能中奖一次）；此时循环的次数不好说，至少是drew award count

Draw执行完毕后，下一届的奖品列表将会被初始化。

## Claim
用户通过Claim方法领奖。

可以在活动进行中领取当前已经获得的奖励。

该操作将会更新用户的OwnLottery中的`claimed_award_amount`字段。

## Redeem
用户通过Redeem方法赎回自己锁定的ELF。

该操作将会更新用户的OwnLottery中的`is_redeemed`字段。

## View方法说明
Lottery合约中有四个主要的MappedState：
- AwardMap，key是award id（每个奖项都有一个award id），value是该奖项的详情，如与该奖项唯一绑定的中奖码（开奖后绑定），是否已经被领取等，使用`GetAward`方法查询
- PeriodAwardMap，key是period id，value是该期的信息，包括起始时间、award id范围等，使用`PeriodAward`方法进行查询
- LotteryMap，key是lottery code，value是该lottery的信息，如该抽奖码所中的award id list，最近领奖领到哪个award id等，使用`GetLottery`方法进行查询
- OwnLotteryMap，key是用户地址，value是该用户相关的信息，如锁了多少仓、有哪些抽奖码、中奖总数额、已领取总数额等，使用`GetOwnLottery`方法进行查询

除此之外：
- GetAwardListByUserAddress，可以查询某个用户的所有获奖奖项
- GetLotteryCodeListByUserAddress，查询用户所拥有的的抽奖码（通过GetOwnLottery也可以查到）
- GetStakingAmount，查询用户锁定了多少代币（通过GetOwnLottery也可以查到）
- GetTotalLotteryCount，查询现在已经兑换了多少抽奖码
- GetCurrentPeriodId，查询即将开奖的期数
- GetAwardListByPeriodId，查询某一期所设置的奖项

## 前端重点关注的View方法

### 获取活动相关时间信息
便于倒计时
- GetStartTimestamp，活动开始时间
- GetShutdownTimestamp，活动结束时间
- GetRedeemTimestamp，开始赎回时间

input都是Empty，output都是google.protobuf.Timestamp

### 获取用户相关信息
使用`GetOwnLottery`可以获取OwnLottery结构：
```Protobuf
message OwnLottery {
    repeated int64 lottery_code_list = 1;
    int64 total_staking_amount = 2;
    int64 total_award_amount = 3;
    int64 claimed_award_amount = 4;
    bool is_redeemed = 5;
}
```
包括：
- 用户所拥有的抽奖码
- 用户总锁仓ELF数量
- 用户获得的ELF奖金总数
- 用户已领取的ELF奖金数
- 用户是否已经赎回

使用`GetAwardListByUserAddress`可以获取某用户当前获得的奖项，output是AwardList类型，相关结构为：
```Protobuf
message Award {
    int64 award_id = 1;
    int64 award_amount = 2;
    int64 lottery_code = 3;
    bool is_claimed = 4;
    aelf.Address owner = 5;
}

message AwardList {
    repeated Award value = 1;
}
```
Award包括：
- Id，唯一标识
- ELF数额
- 该奖项绑定的抽奖码，如果没开奖就是0，不过通过这个方法返回的都是已经开奖给当前用户的
- 是否已经被领取

使用`GetAwardAmountMap`获得用户的每个抽奖码对应的中奖总数（毕竟抽奖码可以多次中奖），返回值是AwardAmountMap，就是长整型对长整型的字典
```Protobuf
message AwardAmountMap {
    map<int64, int64> value = 1;// Lottery Code -> Award Amount
}
```

在显示My Lottery Code板块时：
- 已锁仓这一列，第一个是100，后面的都是1000（不然不显示了？）
- 最后一列直接用`GetAwardAmountMap`方法就行了
- 获得用户锁仓数和获奖数用`GetOwnLottery`

以上方法的input都是aelf.Address

### 获取开奖相关信息
使用`GetPeriodAward`方法获取某一期的基本信息。
```Protobuf
message PeriodAward {
    int32 period_id = 1;
    google.protobuf.Timestamp start_timestamp = 2;
    google.protobuf.Timestamp end_timestamp = 3;// Also draw timestamp
    aelf.Hash use_random_hash = 4;
    int64 start_award_id = 5;
    int64 end_award_id = 6;
}
```
包括：
- 期数，1到8，不过前端显示需要对应成开奖日期
- 本期开始时间
- 本期结束时间（下一期的开始时间）
- 本期使用的随机数
- 本期第一个奖项的id
- 本期最后一个奖项的id

使用`GetAwardList`方法获取某一期所设置的所有奖项的信息，返回值是AwardList，input是GetAwardListInput：
```Protobuf
message GetAwardListInput {
    int32 period_id = 1;
    int64 start_index = 2;
    int32 count = 3;
}
```
start_index和count两个参数用于分页，前者专指这一期的index，对于每期都是从0开始。
count不填则返回从start_index开始的所有award。

前端展示开奖公示的表格用`GetAwardList`方法即可：
- 日期使用period_id做对应
- Lottery Code -> lottery_code
- Reward -> award_amount
- Owner -> owner

用户勾选My Lottery Code后，筛选出Owner是用户的给他显示即可。

