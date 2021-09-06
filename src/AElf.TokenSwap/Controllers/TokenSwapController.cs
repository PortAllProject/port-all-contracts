using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Contracts.Bridge;
using AElf.Contracts.Lottery;
using AElf.Contracts.MultiToken;
using AElf.TokenSwap.Dtos;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace AElf.TokenSwap.Controllers
{
    [ApiController]
    [Route("api/v1.0/swap")]
    public class TokenSwapController : ControllerBase
    {
        private readonly ILogger<TokenSwapController> _logger;

        private readonly ITokenSwapStore<SendingInfo> _tokenSwapStore;
        private readonly ConfigOptions _configOptions;

        public TokenSwapController(IOptionsSnapshot<ConfigOptions> configOptions,
            ITokenSwapStore<SendingInfo> tokenSwapStore, ILogger<TokenSwapController> logger)
        {
            _tokenSwapStore = tokenSwapStore;
            _logger = logger;
            _configOptions = configOptions.Value;
        }

        [HttpPost("record")]
        public async Task<ResponseDto> InsertSendingInfo(SendingInfoDto sendingInfoDto)
        {
            _logger.LogInformation($"Inserting: {JsonSerializer.Serialize(sendingInfoDto)}");

            var sendingInfo = new SendingInfo
            {
                ReceiptId = sendingInfoDto.ReceiptId,
                SendingTime = sendingInfoDto.SendingTime,
                SendingTxId = sendingInfoDto.SendingTxId
            };

            await _tokenSwapStore.SetAsync(sendingInfo.ReceiptId.ToString(), sendingInfo);

            return new ResponseDto
            {
                Message = $"Added sending info of receipt id {sendingInfo.ReceiptId}"
            };
        }

        [HttpGet("get")]
        public async Task<List<ReceiptInfoDto>> GetReceiptInfoList(string receivingAddress)
        {
            var swapId = Hash.LoadFromHex(_configOptions.SwapId);

            var nodeManager = new NodeManager(_configOptions.BlockChainEndpoint);
            var tx = nodeManager.GenerateRawTransaction(_configOptions.AccountAddress,
                _configOptions.BridgeContractAddress,
                "GetSwappedReceiptInfoList", new GetSwappedReceiptInfoListInput
                {
                    SwapId = swapId,
                    ReceivingAddress = Address.FromBase58(receivingAddress)
                });
            var result = await nodeManager.ApiClient.ExecuteTransactionAsync(new ExecuteTransactionDto
            {
                RawTransaction = tx
            });
            var receiptInfoList = new ReceiptInfoList();
            receiptInfoList.MergeFrom(ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(result)));

            var receiptInfoDtoList = new List<ReceiptInfoDto>();
            foreach (var receiptInfo in receiptInfoList.Value)
            {
                var sendingInfo = await _tokenSwapStore.GetAsync(receiptInfo.ReceiptId.ToString());
                receiptInfoDtoList.Add(new ReceiptInfoDto
                {
                    ReceiptId = receiptInfo.ReceiptId,
                    Amount = receiptInfo.Amount,
                    ReceivingTime = receiptInfo.ReceivingTime == null
                        ? string.Empty
                        : (receiptInfo.ReceivingTime.Seconds * 1000).ToString(),
                    ReceivingAddress = receivingAddress,
                    ReceivingTxId = receiptInfo.ReceivingTxId?.ToHex() ?? string.Empty,
                    SendingTime = sendingInfo?.SendingTime ?? string.Empty,
                    SendingTxId = sendingInfo?.SendingTxId ?? string.Empty,
                });
            }

            return receiptInfoDtoList;
        }

        [HttpGet("get_swap_info")]
        public async Task<TokenSwapInfoDto> GetSwapInfo()
        {
            var tokenSwapInfo = new TokenSwapInfoDto();

            var nodeManager = new NodeManager(_configOptions.BlockChainEndpoint);

            // get Bridge Contract balance.
            {
                var txBridge = nodeManager.GenerateRawTransaction(_configOptions.AccountAddress,
                    _configOptions.TokenContractAddress,
                    "GetBalance", new GetBalanceInput
                    {
                        Owner = Address.FromBase58(_configOptions.BridgeContractAddress),
                        Symbol = "ELF"
                    });
                var resultBridge = await nodeManager.ApiClient.ExecuteTransactionAsync(new ExecuteTransactionDto
                {
                    RawTransaction = txBridge
                });
                var getBalanceOutputBridge = new GetBalanceOutput();
                getBalanceOutputBridge.MergeFrom(
                    ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(resultBridge)));
                tokenSwapInfo.BridgeContractBalance = getBalanceOutputBridge.Balance;
            }

            var currentPeriodId = 0L;

            {
                var txBridge = nodeManager.GenerateRawTransaction(_configOptions.AccountAddress,
                    _configOptions.LotteryContractAddress,
                    "GetCurrentPeriodId", new Empty());
                var result = await nodeManager.ApiClient.ExecuteTransactionAsync(new ExecuteTransactionDto
                {
                    RawTransaction = txBridge
                });
                var count = new Int32Value();
                count.MergeFrom(ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(result)));
                currentPeriodId = count.Value;
            }

            {
                var txLottery = nodeManager.GenerateRawTransaction(_configOptions.AccountAddress,
                    _configOptions.LotteryContractAddress,
                    "GetPeriodAward", new Int64Value {Value = currentPeriodId});
                var resultAward = await nodeManager.ApiClient.ExecuteTransactionAsync(new ExecuteTransactionDto
                {
                    RawTransaction = txLottery
                });
                var periodAward = new PeriodAward();
                periodAward.MergeFrom(ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(resultAward)));
                tokenSwapInfo.CurrentPeriodId = periodAward.PeriodId;
                tokenSwapInfo.CurrentPeriodStartTimestamp = periodAward.StartTimestamp.ToString();
            }

            {
                var txBridge = nodeManager.GenerateRawTransaction(_configOptions.AccountAddress,
                    _configOptions.BridgeContractAddress,
                    "GetReceiptCount", new Empty());
                var resultBridge = await nodeManager.ApiClient.ExecuteTransactionAsync(new ExecuteTransactionDto
                {
                    RawTransaction = txBridge
                });
                var count = new Int64Value();
                count.MergeFrom(ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(resultBridge)));
                tokenSwapInfo.TransmittedReceiptCount = count.Value;
            }

            var file = _configOptions.LockAbiFilePath;
            if (!string.IsNullOrEmpty(file))
            {
                if (!System.IO.File.Exists(file))
                {
                    _logger.LogError($"Cannot found file {file}");
                }

                var lockAbi = ReadJson(file, "abi");

                var lockMappingContractAddress = _configOptions.LockMappingContractAddress;
                var web3ManagerForLock = new Web3Manager(_configOptions.EthereumUrl, lockMappingContractAddress,
                    _configOptions.EthereumPrivateKey, lockAbi);
                var lockTimes = await web3ManagerForLock.GetFunction(lockMappingContractAddress, "receiptCount")
                    .CallAsync<long>();
                tokenSwapInfo.CreatedReceiptCount = lockTimes;
            }

            return tokenSwapInfo;
        }

        public static string ReadJson(string jsonfile, string key)
        {
            using var file = System.IO.File.OpenText(jsonfile);
            using var reader = new JsonTextReader(file);
            var o = (JObject) JToken.ReadFrom(reader);
            var value = o[key]?.ToString();
            return value;
        }
    }
}