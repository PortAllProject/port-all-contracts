using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        private Hash GetSwapId(long recorderId)
        {
            var swapInformation = _configOptions.SwapList.Single(i => i.RecorderId == recorderId);
            return Hash.LoadFromHex(swapInformation.SwapId);
        }

        [HttpGet("get")]
        public async Task<List<ReceiptInfoDto>> GetReceiptInfoList(string receivingAddress)
        {
            return await GetReceiptInfoList(0, receivingAddress);
        }

        [HttpGet("get0")]
        public async Task<List<ReceiptInfoDto>> GetReceiptInfoList0(string receivingAddress)
        {
            return await GetReceiptInfoList(0, receivingAddress);
        }

        [HttpGet("get1")]
        public async Task<List<ReceiptInfoDto>> GetReceiptInfoList1(string receivingAddress)
        {
            return await GetReceiptInfoList(1, receivingAddress);
        }

        [HttpGet("get2")]
        public async Task<List<ReceiptInfoDto>> GetReceiptInfoList2(string receivingAddress)
        {
            return await GetReceiptInfoList(2, receivingAddress);
        }

        private async Task<List<ReceiptInfoDto>> GetReceiptInfoList(long recorderId, string receivingAddress)
        {
            var swapId = GetSwapId(recorderId);

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

            tokenSwapInfo.TransmittedReceiptCounts = new List<long>();
            for (var i = 0; i < _configOptions.SwapList.Count; i++)
            {
                var txBridge = nodeManager.GenerateRawTransaction(_configOptions.AccountAddress,
                    _configOptions.BridgeContractAddress,
                    "GetReceiptCount", new Int64Value {Value = i});
                var resultBridge = await nodeManager.ApiClient.ExecuteTransactionAsync(new ExecuteTransactionDto
                {
                    RawTransaction = txBridge
                });
                var count = new Int64Value();
                count.MergeFrom(ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(resultBridge)));
                tokenSwapInfo.TransmittedReceiptCounts.Add(count.Value);
            }

            {
                var txElection = nodeManager.GenerateRawTransaction(_configOptions.AccountAddress,
                    _configOptions.ElectionContractAddress,
                    "GetVotersCount", new Empty());
                var resultVoters = await nodeManager.ApiClient.ExecuteTransactionAsync(new ExecuteTransactionDto
                {
                    RawTransaction = txElection
                });
                var count = new Int64Value();
                count.MergeFrom(ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(resultVoters)));
                tokenSwapInfo.VotersCount = count.Value;
            }

            {
                var txElection = nodeManager.GenerateRawTransaction(_configOptions.AccountAddress,
                    _configOptions.ElectionContractAddress,
                    "GetVotesAmount", new Empty());
                var resultVoters = await nodeManager.ApiClient.ExecuteTransactionAsync(new ExecuteTransactionDto
                {
                    RawTransaction = txElection
                });
                var count = new Int64Value();
                count.MergeFrom(ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(resultVoters)));
                tokenSwapInfo.VotesCount = (double) count.Value / 1_00000000;
            }

            tokenSwapInfo.BridgeContractBalances = new List<double>();
            foreach (var swapInformation in _configOptions.SwapList.OrderBy(i => i.RecorderId))
            {
                var tx = nodeManager.GenerateRawTransaction(_configOptions.AccountAddress,
                    _configOptions.BridgeContractAddress,
                    "GetSwapPair", new GetSwapPairInput
                    {
                        SwapId = Hash.LoadFromHex(swapInformation.SwapId),
                        TargetTokenSymbol = swapInformation.TokenSymbols.First()
                    });
                var result = await nodeManager.ApiClient.ExecuteTransactionAsync(new ExecuteTransactionDto
                {
                    RawTransaction = tx
                });
                var swapPair = new SwapPair();
                swapPair.MergeFrom(ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(result)));
                var foo = 1L;
                for (var i = 0; i < swapInformation.Decimal; i++)
                {
                    foo *= 10;
                }

                tokenSwapInfo.BridgeContractBalances.Add((double) swapPair.DepositAmount / foo);
            }

            tokenSwapInfo.CreatedReceiptCounts = new List<long>();
            foreach (var swapInformation in _configOptions.SwapList.OrderBy(i => i.RecorderId))
            {
                var file = _configOptions.LockAbiFilePath;
                if (!string.IsNullOrEmpty(file))
                {
                    if (!System.IO.File.Exists(file))
                    {
                        _logger.LogError($"Cannot found file {file}");
                    }

                    var lockAbi = ReadJson(file, "abi");

                    var lockMappingContractAddress = swapInformation.LockMappingContractAddress;
                    var web3ManagerForLock = new Web3Manager(swapInformation.NodeUrl, lockMappingContractAddress,
                        _configOptions.EthereumPrivateKey, lockAbi);
                    var lockTimes = await web3ManagerForLock.GetFunction(lockMappingContractAddress, "receiptCount")
                        .CallAsync<long>();
                    tokenSwapInfo.CreatedReceiptCounts.Add(lockTimes);
                }
            }

            return tokenSwapInfo;
        }

        private static string ReadJson(string jsonfile, string key)
        {
            using var file = System.IO.File.OpenText(jsonfile);
            using var reader = new JsonTextReader(file);
            var o = (JObject) JToken.ReadFrom(reader);
            var value = o[key]?.ToString();
            return value;
        }
    }
}