using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Bridge;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public class RecordReceiptsDataProvider : IDataProvider, ISingletonDependency
    {
        public const string Title = "record_receipts";

        private readonly ILogger<RecordReceiptsDataProvider> _logger;
        private readonly INethereumManagerFactory _nethereumManagerFactory;
        private readonly ConfigOptions _configOptions;

        public RecordReceiptsDataProvider(ILogger<RecordReceiptsDataProvider> logger,
            INethereumManagerFactory nethereumManagerFactory, IOptionsSnapshot<ConfigOptions> configOptions)
        {
            _logger = logger;
            _nethereumManagerFactory = nethereumManagerFactory;
            _configOptions = configOptions.Value;
        }

        public async Task<string> GetDataAsync(Hash queryId, string title = null, List<string> options = null)
        {
            if (options == null)
            {
                _logger.LogError($"No data of {queryId} for revealing.");
                return string.Empty;
            }

            if (options.Count != 2)
            {
                _logger.LogError($"Incorrect options count.");
                return string.Empty;
            }

            _logger.LogInformation("About to handle record receipt hashes for swapping tokens.");
            var recordReceiptHashInput =
                await GetReceiptHashMap(long.Parse(options[0]), long.Parse(options[1]));
            _logger.LogInformation($"RecordReceiptHashInput: {recordReceiptHashInput}");
            return recordReceiptHashInput;
        }

        private async Task<string> GetReceiptHashMap(long start, long end)
        {
            var receiptInfos = await GetReceiptInfosAsync(start, end);
            var receiptHashes = new List<Hash>();
            for (var i = 0; i <= end - start; i++)
            {
                var amountHash = GetHashTokenAmountData(receiptInfos[i].Amount.ToString(), 32, true);
                var targetAddressHash = HashHelper.ComputeFrom(receiptInfos[i].TargetAddress);
                var receiptIdHash = HashHelper.ComputeFrom(i + start);
                var hash = HashHelper.ConcatAndCompute(amountHash, targetAddressHash, receiptIdHash);
                receiptHashes.Add(hash);
            }

            var input = new ReceiptHashMap
            {
                RecorderId = _configOptions.RecorderId
            };
            for (var i = 0; i <= end - start; i++)
            {
                var index = (int) (i + start);
                input.Value.Add(index, receiptHashes[i].ToHex());
            }

            return input.ToString();
        }

        private Hash GetHashTokenAmountData(string stringAmount, int originTokenSizeInByte, bool isBigEndian)
        {
            var amount = decimal.Parse(stringAmount);
            var preHolderSize = originTokenSizeInByte - 16;
            int[] amountInIntegers;
            if (isBigEndian)
            {
                amountInIntegers = decimal.GetBits(amount).Reverse().ToArray();
                if (preHolderSize < 0)
                    amountInIntegers = amountInIntegers.TakeLast(originTokenSizeInByte / 4).ToArray();
            }
            else
            {
                amountInIntegers = decimal.GetBits(amount).ToArray();
                if (preHolderSize < 0)
                    amountInIntegers = amountInIntegers.Take(originTokenSizeInByte / 4).ToArray();
            }

            var amountBytes = new List<byte>();

            amountInIntegers.Aggregate(amountBytes, (cur, i) =>
            {
                cur.AddRange(i.ToBytes(isBigEndian));
                return cur;
            });

            if (preHolderSize > 0)
            {
                var placeHolder = Enumerable.Repeat(new byte(), preHolderSize).ToArray();
                amountBytes = isBigEndian
                    ? placeHolder.Concat(amountBytes).ToList()
                    : amountBytes.Concat(placeHolder).ToList();
            }

            return HashHelper.ComputeFrom(amountBytes.ToArray());
        }

        private async Task<List<ReceiptInfo>> GetReceiptInfosAsync(long start, long end)
        {
            var receiptInfoList = new List<ReceiptInfo>();
            var getReceiptInfoFunction = _nethereumManagerFactory.CreateManager(new LockMappingContractNameProvider())
                .GetFunction("getReceiptInfo");
            for (var i = start; i <= end; i++)
            {
                var receiptInfo = await getReceiptInfoFunction.CallDeserializingToObjectAsync<ReceiptInfo>(i);
                _logger.LogInformation($"Got receipt info of id {i}: {receiptInfo}");
                receiptInfoList.Add(receiptInfo);
            }

            return receiptInfoList;
        }
    }
}