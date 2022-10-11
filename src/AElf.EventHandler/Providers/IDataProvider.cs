using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using AElf.Client.Bridge;
using AElf.Client.Core.Options;
using AElf.Contracts.Bridge;
using AElf.Nethereum.Bridge;
using AElf.Nethereum.Core;
using AElf.Nethereum.Core.Options;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.EventHandler;

public interface IDataProvider
{
    Task<string> GetDataAsync(Hash queryId, string title = null, List<string> options = null);
}

public class DataProvider : IDataProvider, ISingletonDependency
{
    private readonly Dictionary<Hash, string> _dictionary;
    private readonly ILogger<DataProvider> _logger;
    private readonly string _bridgeAbi;
    private Web3Manager _web3ManagerForLock;
    private BridgeOptions _bridgeOptions;
    private IBridgeInService _bridgeInService;

    public DataProvider(
        ILogger<DataProvider> logger, 
        IOptionsSnapshot<BridgeOptions> bridgeOptions,
        IBridgeInService bridgeInService)
    {
        _logger = logger;
        _bridgeOptions = bridgeOptions.Value;
        _bridgeInService = bridgeInService;
        _dictionary = new Dictionary<Hash, string>();
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

        if (title.StartsWith("record_receipts") && options.Count == 2)
        {
            var swapId = title.Split('_').Last();
            _logger.LogInformation($"Trying to query record receipt data. Swap id: {swapId}");
            var bridgeItem = _bridgeOptions.BridgesIn.Single(c => c.SwapId == swapId);
            _logger.LogInformation("About to handle record receipt hashes for swapping tokens.");
            var recordReceiptHashInput =
                await GetReceiptHashMap(Hash.LoadFromHex(swapId), bridgeItem, long.Parse(options[0].Split(".").Last()),
                    long.Parse(options[1].Split(".").Last()));
            _logger.LogInformation($"RecordReceiptHashInput: {recordReceiptHashInput}");
            _dictionary[queryId] = recordReceiptHashInput;
            return recordReceiptHashInput;
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

    private async Task<string> GetReceiptHashMap(Hash swapId, BridgeItemIn bridgeItem, long start, long end)
    {
        var token = _bridgeOptions.BridgesIn.Single(c => c.SwapId == swapId.ToHex()).OriginToken;
        var chainId = _bridgeOptions.BridgesIn.Single(c => c.SwapId == swapId.ToHex()).TargetChainId;
        var receiptInfos = await _bridgeInService.GetSendReceiptInfosAsync(bridgeItem.ChainId,bridgeItem.EthereumBridgeInContractAddress, token, chainId,start,end);
        var receiptHashes = new List<Hash>();
        for (var i = 0; i <= end - start; i++)
        {
            var amountHash = HashHelper.ComputeFrom((receiptInfos.Receipts[i].Amount).ToString());
            var targetAddressHash = HashHelper.ComputeFrom(receiptInfos.Receipts[i].TargetAddress);
            var receiptIdHash = HashHelper.ComputeFrom(receiptInfos.Receipts[i].ReceiptId);
            var hash = HashHelper.ConcatAndCompute(amountHash, targetAddressHash, receiptIdHash);
            receiptHashes.Add(hash);
        }
        
        var input = new ReceiptHashMap
        {
            SwapId = swapId.ToHex()
        };
        for (var i = 0; i <= end - start; i++)
        {
            input.Value.Add(receiptInfos.Receipts[i].ReceiptId, receiptHashes[i].ToHex());
        }
        
        return input.ToString();
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
            var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
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