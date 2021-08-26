1. aelf的私钥文件放在aelf/keys文件夹下；
2. 在appsettings.json的Config-AccountAddress中配置节点的地址；
3. 在appsettings.json的Contracts中配置Oracle和Rpoert合约的地址。

# Draw Lottery Helper
配置项：
- IsDrawLottery： 该客户端是否负责自动开奖
- AccountAddress & AccountPassword：Lottery合约的Admin的地址和密码
- LotteryContractAddress：Lottery合约地址
- StartTimestamp：抽奖活动开始时间
- IntervalMinutes：每一期时间，分钟计
- LatestDrewPeriod：最近一次开奖期数（默认为0，如果活动中奖重启过，需要把最近开奖的期数放进去，防止立刻就要开奖）