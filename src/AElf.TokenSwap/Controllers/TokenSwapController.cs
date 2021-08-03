using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.Client.Dto;
using AElf.Contracts.Bridge;
using AElf.TokenSwap.Dtos;
using AElf.Types;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

        [HttpPost("sending_info")]
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
                Code = MessageHelper.GetCode(MessageHelper.Message.Success),
                Message = $"Added sending info of receipt id {sendingInfo.ReceiptId}"
            };
        }

        [HttpGet("get_receipt_info")]
        public async Task<List<ReceiptInfoDto>> GetReceiptInfoList(string receivingAddress)
        {
            var swapId = Hash.LoadFromBase64(_configOptions.SwapId);

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
                    ReceivingTime = receiptInfo.ReceivingTime.ToString(),
                    ReceivingAddress = receivingAddress,
                    ReceivingTxId = receiptInfo.ReceivingTxId.ToHex(),
                    SendingTime = sendingInfo.SendingTime,
                    SendingTxId = sendingInfo.SendingTxId
                });
            }

            return receiptInfoDtoList;
        }
    }
}