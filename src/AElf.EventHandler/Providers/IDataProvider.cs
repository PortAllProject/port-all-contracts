using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MTRecorder;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler
{
    public interface IDataProvider
    {
        Task<string> GetDataAsync(Hash queryId, string title = null, List<string> options = null);
    }

    public class DataProvider : IDataProvider, ISingletonDependency
    {
        private readonly Dictionary<Hash, string> _dictionary;
        private readonly ILogger<DataProvider> _logger;
        private readonly ContractAddressOptions _contractAddressOptions;
        private readonly EthereumConfigOptions _ethereumConfigOptions;
        private readonly ConfigOptions _configOptions;
        private readonly string _lockAbi;
        private readonly string _merkleGeneratorAbi;

        public DataProvider(ILogger<DataProvider> logger, IOptionsSnapshot<EthereumConfigOptions> ethereumConfigOptions,
            IOptionsSnapshot<ConfigOptions> configOptions, IOptionsSnapshot<ContractAbiOptions> contractAbiOptions,
            IOptionsSnapshot<ContractAddressOptions> contractAddressOptions)
        {
            _logger = logger;
            _contractAddressOptions = contractAddressOptions.Value;
            var contractAbiOptions1 = contractAbiOptions.Value;
            _configOptions = configOptions.Value;
            _ethereumConfigOptions = ethereumConfigOptions.Value;
            _dictionary = new Dictionary<Hash, string>();
            {
                var file = contractAbiOptions1.LockAbiFilePath;
                if (!string.IsNullOrEmpty(file))
                    _lockAbi = JsonHelper.ReadJson(file, "abi");
            }
            {
                var file = contractAbiOptions1.MerkleGeneratorAbiFilePath;
                if (!string.IsNullOrEmpty(file))
                    _merkleGeneratorAbi = JsonHelper.ReadJson(file, "abi");
            }
        }

        public async Task<string> GetDataAsync(Hash queryId, string title = null, List<string> options = null)
        {
            if (title == "invalid")
            {
                return "0";
            }

            if (_dictionary.TryGetValue(queryId, out var data))
            {
                return data;
            }

            if (title == null || options == null)
            {
                _logger.LogError($"No data of {queryId} for revealing.");
                return string.Empty;
            }

            if (title == "swap")
            {
                return await GetRecordMerkleTreeInput();
            }

            string result;

            if (!title.Contains('|'))
            {
                result = await GetSingleUrlDataAsync(title, options);
            }
            else
            {
                var urls = title.Split('|');
                var urlAttributes = options.Select(a => a.Split('|')).ToList();
                var dataList = new List<decimal>();
                for (var i = 0; i < urls.Length; i++)
                {
                    var singleData =
                        await GetSingleUrlDataAsync(urls[i], urlAttributes.Select(a => a[i]).ToList());
                    if (singleData.Contains("\""))
                    {
                        singleData = singleData.Replace("\"", "");
                    }

                    if (decimal.TryParse(singleData, out var decimalData))
                    {
                        _logger.LogInformation($"Add {singleData} to data list.");
                        dataList.Add(decimalData);
                    }
                    else
                    {
                        throw new Exception($"Error during paring {singleData} to decimal");
                    }
                }

                result = Aggregate(dataList);
            }

            _dictionary[queryId] = result;
            return result;
        }

        private async Task<string> GetRecordMerkleTreeInput()
        {
            var lockMappingContractAddress = _configOptions.LockMappingContractAddress;
            var merkleGeneratorContractAddress = _configOptions.MerkleGeneratorContractAddress;
            var web3ManagerForLock = new Web3Manager(_ethereumConfigOptions.Url, lockMappingContractAddress,
                _ethereumConfigOptions.PrivateKey, _lockAbi);
            var web3ManagerForMerkleGenerator = new Web3Manager(_ethereumConfigOptions.Url,
                merkleGeneratorContractAddress,
                _ethereumConfigOptions.PrivateKey, _merkleGeneratorAbi);
            var node = new NodeManager(_configOptions.BlockChainEndpoint, _configOptions.AccountAddress,
                _configOptions.AccountPassword);
            var merkleTreeRecorderContractAddress = _contractAddressOptions.ContractAddressMap["MTRecorder"];

            var lockTimes = await web3ManagerForLock.GetFunction(lockMappingContractAddress, "receiptCount")
                .CallAsync<long>();
            var lastRecordedLeafIndex = node.QueryView<Int64Value>(_configOptions.AccountAddress,
                merkleTreeRecorderContractAddress, "GetLastRecordedLeafIndex",
                new RecorderIdInput
                {
                    RecorderId = _configOptions.RecorderId
                }).Value;
            if (lockTimes <= lastRecordedLeafIndex + 1)
                // No need to record merkle tree.
                return string.Empty;

            var satisfiedTreeCount = node.QueryView<Int64Value>(_configOptions.AccountAddress,
                _contractAddressOptions.ContractAddressMap["MTRecorder"], "GetSatisfiedTreeCount",
                new RecorderIdInput
                {
                    RecorderId = _configOptions.RecorderId
                }).Value;
            var expectCount = (satisfiedTreeCount + 1) * _configOptions.MaximumLeafCount;
            var result = await web3ManagerForMerkleGenerator
                .GetFunction(merkleGeneratorContractAddress, "getMerkleTree")
                .CallAsync<Tuple<byte[], long, long, long, byte[][]>>(expectCount);
            var root = result.Item1;
            var firstReceiptId = result.Item2;
            var leafCount = result.Item3;
            var lastLeafIndex = firstReceiptId + leafCount - 1;

            return new RecordMerkleTreeInput
            {
                RecorderId = _configOptions.RecorderId,
                LastLeafIndex = lastLeafIndex,
                MerkleTreeRoot = Hash.LoadFromByteArray(root)
            }.ToString();
        }

        private string Aggregate(List<decimal> dataList)
        {
            var finalPrice = dataList.OrderBy(p => p).ToList()[dataList.Count / 2]
                .ToString(CultureInfo.InvariantCulture);

            _logger.LogInformation($"Final price: {finalPrice}");

            return finalPrice;
        }

        public async Task<string> GetSingleUrlDataAsync(string url, List<string> attributes)
        {
            _logger.LogInformation($"Querying {url} for attributes {attributes.First()} etc..");

            var data = string.Empty;
            var response = string.Empty;
            try
            {
                var client = new HttpClient {Timeout = TimeSpan.FromMinutes(2)};
                using var responseMessage = await client.GetHttpResponseMessageWithRetryAsync(url, _logger);
                response = await responseMessage.Content.ReadAsStringAsync();
            }
            catch (Exception e)
            {
                _logger.LogError($"Error during querying: {e.Message}");
            }

            try
            {
                _logger.LogInformation($"Trying to parse response to json: {response}");

                if (response != string.Empty)
                {
                    data = ParseJson(response, attributes);
                }
            }
            catch (Exception e)
            {
                _logger.LogError($"Error during parsing json: {response}\n{e.Message}");
                throw;
            }

            if (string.IsNullOrEmpty(data))
            {
                data = "0";
                _logger.LogError($"Failed to get {attributes.First()} from {response}, will just return 0.");

            }

            return data;
        }

        private string ParseJson(string response, List<string> attributes)
        {
            var jsonDoc = JsonDocument.Parse(response);
            var data = string.Empty;

            foreach (var attribute in attributes)
            {
                if (!attribute.Contains('/'))
                {
                    if (jsonDoc.RootElement.TryGetProperty(attribute, out var targetElement))
                    {
                        if (data == string.Empty)
                        {
                            data = targetElement.GetRawText();
                        }
                        else
                        {
                            data += $";{targetElement.GetRawText()}";
                        }
                    }
                    else
                    {
                        return data;
                    }
                }
                else
                {
                    var attrs = attribute.Split('/');
                    var targetElement = jsonDoc.RootElement.GetProperty(attrs[0]);
                    foreach (var attr in attrs.Skip(1))
                    {
                        if (!targetElement.TryGetProperty(attr, out targetElement))
                        {
                            return attr;
                        }
                    }

                    if (data == string.Empty)
                    {
                        data = targetElement.GetRawText();
                    }
                    else
                    {
                        data += $";{targetElement.GetRawText()}";
                    }
                }
            }

            return data;
        }
    }
}