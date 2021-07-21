# Lottery Contract
用于aelf主网换币活动后的抽奖活动。

## Initialize
初始化该合约时，至少需要输入：
- start_timestamp，活动开始时间（从这个时间点后，用户可以锁定ELF代币获得抽奖码）
- shutdown_timestamp，活动结束时间（从这个时间点后，用户不再能锁定ELF代币获得抽奖码）
- redeem_timestamp，开始赎回时间（从这个时间点后，用户可以赎回之前锁定的ELF代币）

以上三个时间必须为递增，后两者可以相同。

可选收入默认每期的ELF奖项：
- default_award_list

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

## Stake
用户通过Stake方法锁定代币，其参数为用户本次锁定的代币数额。

Lottery合约会自动为用户兑换出合适数量的代币。

|  用户锁定代币   | 兑换抽奖码个数  |
|  ----  | ----  |
| 99  | 0 |
| 999  | 1 |
| 1100  | 2 |
| 20100  | 21 |
| 99999  | 21 |

用户可以分批多次锁定不同额度的代币，得到的抽奖码个数只与staking的**总数额**相关。

## Draw
Lottery合约的Admin通过Draw方法开奖。

DrawInput包含两个参数：
- period_id（必填）：填入要开奖的期的期数，主要为了防止重复开奖；
- next_award_list（选填）：用于修改下一期的奖品，不填的话使用`GetDefaultAwardList()`中的设置。

开奖结束后：
- 本期所有`Award`的`lottery_code`字段将会被分配上获奖的抽奖码；
- 相关的`Lottery`的`award_id_list`字段也会加入刚获奖奖项的`award_id`。

Draw执行完毕后，下一届的奖品列表将会被初始化。

## Claim
用户通过Claim方法领奖。

可以在活动进行中领取当前已经获得的奖励。

该操作将会更新用户的OwnLottery中的`claimed_award_amount`字段。

## Redeem
用户通过Redeem方法赎回自己锁定的ELF。

该操作将会更新用户的OwnLottery中的`is_redeemed`字段。