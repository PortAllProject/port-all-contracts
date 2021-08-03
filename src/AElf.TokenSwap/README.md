# 添加Sending Info

在用户点击Send之后，把以太坊交易id和当前时间post到服务上：
- receipt_id
- sending_tx_id
- sending_time

POST:

`/api/v1.0/swap/record`

Demo:

`curl -X POST "https://localhost:7079/api/v1.0/swap/record" -H "accept: text/plain; v=1.0" -H "Content-Type: application/json; v=1.0" -d "{\"receipt_id\":18,\"sending_tx_id\":\"0x44432847eccef9e492bbbae32adf494fd670ac8960b9a8083b7df5c864324a0f\",\"sending_time\":\"2021-08-03 17:00:00\"}"`

# 获取Receipt Info

直接使用代币接收人地址获取Receipt Info。

GET:

`/api/v1.0/swap/get`

Demo：

`curl -X GET "https://localhost:7079/api/v1.0/swap/get?receivingAddress=t7nCCoqKrVbyDHNxbAMtEvtG6UoN4HZE9DVPRQG8UrppZppBq" -H "accept: text/plain; v=1.0"`

返回结果：

```json
[
  {
    "receipt_id": 17,
    "sending_tx_id": "0x53fb5598737a7348d18de20a4ec19a585809270ab7ba0dca1fa0f1a8ff18b5b4",
    "sending_time": "test",
    "receiving_tx_id": "cee13d9316bf07384c2b6d81eb5470304d3b83c64db9ca905e21b8e20369c0c7",
    "receiving_time": "\"2021-08-03T09:50:48.139269700Z\"",
    "amount": 1000000000,
    "receiving_address": "t7nCCoqKrVbyDHNxbAMtEvtG6UoN4HZE9DVPRQG8UrppZppBq"
  },
  {
    "receipt_id": 18,
    "sending_tx_id": "0x44432847eccef9e492bbbae32adf494fd670ac8960b9a8083b7df5c864324a0f",
    "sending_time": "2021-08-03 17:00:00",
    "receiving_tx_id": "442035a2f35d57b2ff106284a10596c87835d06044464e7c3f7b8fb8d04b442d",
    "receiving_time": "\"2021-08-03T09:53:28.098672200Z\"",
    "amount": 5000000000,
    "receiving_address": "t7nCCoqKrVbyDHNxbAMtEvtG6UoN4HZE9DVPRQG8UrppZppBq"
  }
]
```

其中sending_tx_id和sending_time两个字段是通过上面的POST API插入的，给string即可。

注：由于Bridge合约需要升级才能记录完整信息，在2021年08月03日之前兑换的代币的ReceiptInfo只能显示`receiving_address`和`amount`。

# 项目配置

`ConnectionStrings.Default`：如果填InMemory，就会用内存字典。

`Urls`：本服务绑定端口号

`Config-BlockChainEndpoint`：区块链节点Endpoint

`Config-AccountAddress`：查询区块链所用账户地址，不用改

`Config-BridgeContractAddress`：Bridge合约地址

`Config-SwapId`：Swap Id

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "ConnectionStrings": {
    "Default": "redis://localhost:6379?db=1"
  },
  "Urls": "https://localhost:7079;http://localhost:7080",
  "Config": {
    "BlockChainEndpoint": "http://18.163.40.216:8000/",
    "AccountAddress": "asoqSF2R4oFJBMqr95TRuaMiUMP5SXdVdwYH5HBQjYqk22CHw",
    "BridgeContractAddress": "2RHf2fxsnEaM3wb6N1yGqPupNZbcCY98LgWbGSFWmWzgEs5Sjo",
    "SwapId": "caaa8140bb484e1074872350687df0b1262436cdec3042539e78eb615b376d5e"
  },
  "AllowedHosts": "*"
}

```