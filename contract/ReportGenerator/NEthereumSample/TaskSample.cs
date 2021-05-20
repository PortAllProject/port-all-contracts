using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.Web3;

namespace ReportGenerator
{
    public static class TaskExamples
    {
        const int UNLOCK_TIMEOUT = 2 * 60; // 2 minutes (arbitrary)
        const int SLEEP_TIME = 5 * 1000; // 5 seconds (arbitrary)
        const int MAX_TIMEOUT = 2 * 60 * 1000; // 2 minutes (arbirtrary)

// These static public variables do not represent a recommended pattern
        public static string LastProtocolVersion = "";
        public static string LastTxHash = "";
        public static Nethereum.RPC.Eth.DTOs.TransactionReceipt LastTxReceipt = null;
        public static HexBigInteger LastMaxBlockNumber = new HexBigInteger(0);

        public static async Task GetProtocolVersionExample(Web3 web3)
        {
            Console.WriteLine("GetProtocolVersionExample:");

            var protocolVersion = await web3.Eth.ProtocolVersion.SendRequestAsync();
            Console.WriteLine("protocolVersion:\t" + protocolVersion.ToString());
            LastProtocolVersion = protocolVersion;
        }

        public static async Task GetMaxBlockExample(Web3 web3)
        {
            Console.WriteLine("GetMaxBlockExample:");

            var maxBlockNumber = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            Console.WriteLine("maxBlockNumber:\t" + maxBlockNumber.Value.ToString());
            LastMaxBlockNumber = maxBlockNumber;
        }

        public static async Task ScanBlocksExample(Web3 web3, ulong startBlockNumber, ulong endBlockNumber)
        {
            Console.WriteLine("ScanBlocksExample:");

            long txTotalCount = 0;
            for (ulong blockNumber = startBlockNumber; blockNumber <= endBlockNumber; blockNumber++)
            {
                var blockParameter = new Nethereum.RPC.Eth.DTOs.BlockParameter(blockNumber);
                var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(blockParameter);
                var trans = block.Transactions;
                int txCount = trans.Length;
                txTotalCount += txCount;
                if (blockNumber % 1000 == 0) Console.Write(".");
                if (blockNumber % 10000 == 0)
                {
                    DateTime blockDateTime = Helpers.UnixTimeStampToDateTime((double) block.Timestamp.Value);
                    Console.WriteLine(blockNumber.ToString() + " " + txTotalCount.ToString() + " " +
                                      blockDateTime.ToString());
                }
            }

            Console.WriteLine();
        }

        public static async Task ScanTxExample(Web3 web3, ulong startBlockNumber, ulong endBlockNumber)
        {
            Console.WriteLine("ScanTxExample:");

            long txTotalCount = 0;
            for (ulong blockNumber = startBlockNumber; blockNumber <= endBlockNumber; blockNumber++)
            {
                var blockParameter = new Nethereum.RPC.Eth.DTOs.BlockParameter(blockNumber);
                var block = await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(blockParameter);
                var trans = block.Transactions;
                int txCount = trans.Length;
                txTotalCount += txCount;
                foreach (var tx in trans)
                {
                    try
                    {
                        var bn = tx.BlockNumber.Value;
                        var th = tx.TransactionHash;
                        var ti = tx.TransactionIndex.Value;

                        var rpt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(th);
                        var status = rpt.Status.Value;

                        var nc = tx.Nonce.Value;
                        var from = tx.From;

                        Console.WriteLine(th.ToString() + " " + ti.ToString() + " " + from.ToString() + " " +
                                          status.ToString());

                        var to = tx.To;
                        if (to == null) to = "to:NULL";
                        var v = tx.Value.Value;
                        var g = tx.Gas.Value;
                        var gp = tx.GasPrice.Value;
                        Console.WriteLine(th.ToString() + " " + ti.ToString() + " " + nc.ToString() + " " +
                                          from.ToString() + " " + to.ToString() + " " + v.ToString() + " " +
                                          g.ToString() + " " + gp.ToString());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ScanTxExample.Tx:\t" + ex.ToString());
                        if (ex.InnerException != null)
                            Console.WriteLine("ScanTxExample.Tx:\t" + ex.InnerException.ToString());
                    }
                }

                Console.WriteLine();
            }
        }

        public static async Task GetAccountBalanceExample(Web3 web3, string accountAddress)
        {
            Console.WriteLine("GetAccountBalanceExample:");

            var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(accountAddress);
            var balanceEther = Web3.Convert.FromWei(balanceWei.Value);
            Console.WriteLine("accountAddress:\t" + accountAddress.ToString());
            Console.WriteLine("balanceEther:\t" + balanceEther.ToString());
        }

        public static async Task ListPersonalAccountsExample(Web3 web3)
        {
            Console.WriteLine("ListPersonalAccountsExample:");

            var accounts = await web3.Personal.ListAccounts.SendRequestAsync();
            foreach (var account in accounts)
            {
                var balanceWei = await web3.Eth.GetBalance.SendRequestAsync(account);
                var balanceEther = Web3.Convert.FromWei(balanceWei.Value);
                Console.WriteLine("account:\t" + account + " balanceEther:\t" + balanceEther.ToString());
            }
        }

        public static async Task SendEtherExample(Web3 web3, string fromAddress, string fromPassword, string toAddress,
            long amountWei)
        {
            Console.WriteLine("SendEtherExample:");

            var unlockResult =
                await web3.Personal.UnlockAccount.SendRequestAsync(fromAddress, fromPassword, UNLOCK_TIMEOUT);
            var sendTxHash =
                await web3.Eth.TransactionManager.SendTransactionAsync(fromAddress, toAddress,
                    new HexBigInteger(amountWei));
            Console.WriteLine("fromAddress:\t" + fromAddress.ToString());
            Console.WriteLine("toAddress:\t" + toAddress.ToString());
            Console.WriteLine("amountWei:\t" + amountWei.ToString());
            Console.WriteLine("sendTxHash:\t" + sendTxHash.ToString());
            LastTxHash = sendTxHash;
        }

        public static async Task WaitForTxReceiptExample(Web3 web3, string txHash)
        {
            Console.WriteLine("WaitForTxReceiptExample:");

            int timeoutCount = 0;
            var txReceipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
            while (txReceipt == null && timeoutCount < MAX_TIMEOUT)
            {
                Console.WriteLine("Sleeping...");
                Thread.Sleep(SLEEP_TIME);
                txReceipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash);
                timeoutCount += SLEEP_TIME;
            }

            Console.WriteLine("timeoutCount " + timeoutCount.ToString());
            LastTxReceipt = txReceipt;
        }

        public static async Task InteractWithExistingContractExample(Web3 web3, string fromAddress, string fromPassword,
            string contractAddress, string contractAbi)
        {
            Console.WriteLine("InteractWithExistingContractExample:");

            var contract = web3.Eth.GetContract(contractAbi, contractAddress);

            var setMessageFunction = contract.GetFunction("setMsg");
            var getMessageFunction = contract.GetFunction("getMsg");

            string nowTimestamp = DateTime.UtcNow.ToString() + " UTC";
            Console.WriteLine("now:\t" + nowTimestamp);

            var unlockResult =
                await web3.Personal.UnlockAccount.SendRequestAsync(fromAddress, fromPassword, UNLOCK_TIMEOUT);
            var txHash1 =
                await setMessageFunction.SendTransactionAsync(fromAddress, new HexBigInteger(900000), null, 1,
                    "Hello World");
            Console.WriteLine("txHash1:\t" + txHash1.ToString());
            var txHash2 =
                await setMessageFunction.SendTransactionAsync(fromAddress, new HexBigInteger(900000), null, 2,
                    nowTimestamp);
            Console.WriteLine("txHash2:\t" + txHash2.ToString());

            var txReceipt2 = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash2);
            int timeoutCount = 0;
            while (txReceipt2 == null && timeoutCount < MAX_TIMEOUT)
            {
                Console.WriteLine("Sleeping...");
                Thread.Sleep(SLEEP_TIME);
                txReceipt2 = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash2);
                timeoutCount += SLEEP_TIME;
            }

            Console.WriteLine("timeoutCount:\t" + timeoutCount.ToString());

            var txReceipt3 = await setMessageFunction.SendTransactionAndWaitForReceiptAsync(fromAddress,
                new HexBigInteger(900000), null, null, 2, nowTimestamp + " Wait");
            Console.WriteLine("txReceipt3:\t" + txReceipt3.TransactionHash.ToString());
            Console.WriteLine("txReceipt3:\t" + txReceipt3.CumulativeGasUsed.Value.ToString());

            var getResult1 = await getMessageFunction.CallAsync<string>(1);
            Console.WriteLine("getResult1:\t" + getResult1.ToString());
            var getResult2 = await getMessageFunction.CallAsync<string>(2);
            Console.WriteLine("getResult2:\t" + getResult2.ToString());
        }

        public static async Task InteractWithExistingContractWithEventsExample(Web3 web3, string fromAddress,
            string fromPassword, string contractAddress, string contractAbi)
        {
            Console.WriteLine("InteractWithExistingContractWithEventsExample:");

            var contract = web3.Eth.GetContract(contractAbi, contractAddress);

            var setMessageFunction = contract.GetFunction("setMsg");
            var getMessageFunction = contract.GetFunction("getMsg");
            var multipliedEvent = contract.GetEvent("MultipliedEvent");
            var newMessageEvent = contract.GetEvent("NewMessageEvent");

            var filterAllMultipliedEvent = await multipliedEvent.CreateFilterAsync();
            var filterAllNewMessageEvent = await newMessageEvent.CreateFilterAsync();

            string nowTimestamp = DateTime.UtcNow.ToString() + " UTC";
            Console.WriteLine("now:\t" + nowTimestamp);

            var unlockResult =
                await web3.Personal.UnlockAccount.SendRequestAsync(fromAddress, fromPassword, UNLOCK_TIMEOUT);
            var txHash1 =
                await setMessageFunction.SendTransactionAsync(fromAddress, new HexBigInteger(900000), null, 1,
                    "Hello World");
            Console.WriteLine("txHash1:\t" + txHash1.ToString());
            var txHash2 =
                await setMessageFunction.SendTransactionAsync(fromAddress, new HexBigInteger(900000), null, 2,
                    nowTimestamp);
            Console.WriteLine("txHash2:\t" + txHash2.ToString());

            var txReceipt2 = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash2);
            int timeoutCount = 0;
            while (txReceipt2 == null && timeoutCount < MAX_TIMEOUT)
            {
                Console.WriteLine("Sleeping...");
                Thread.Sleep(SLEEP_TIME);
                txReceipt2 = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(txHash2);
                timeoutCount += SLEEP_TIME;
            }

            Console.WriteLine("timeoutCount:\t" + timeoutCount.ToString());

            var txReceipt3 = await setMessageFunction.SendTransactionAndWaitForReceiptAsync(fromAddress,
                new HexBigInteger(900000), null, null, 2, nowTimestamp + " Wait");
            Console.WriteLine("txReceipt3:\t" + txReceipt3.TransactionHash.ToString());
            Console.WriteLine("txReceipt3:\t" + txReceipt3.CumulativeGasUsed.Value.ToString());

            var getResult1 = await getMessageFunction.CallAsync<string>(1);
            Console.WriteLine("getResult1:\t" + getResult1.ToString());
            var getResult2 = await getMessageFunction.CallAsync<string>(2);
            Console.WriteLine("getResult2:\t" + getResult2.ToString());

            var logMultipliedEvents =
                await multipliedEvent.GetFilterChanges<FunctionOutputHelpers.MultipliedEventArgs>(
                    filterAllMultipliedEvent);
            foreach (var mea in logMultipliedEvents)
            {
                Console.WriteLine("multipliedEvent:\t" +
                                  mea.Event.sender + " " + mea.Event.oldProduct.ToString() + " " +
                                  mea.Event.value.ToString() + " " + mea.Event.newProduct.ToString());
            }

            var logNewMessageEvents =
                await newMessageEvent.GetFilterChanges<FunctionOutputHelpers.NewMessageEventArgs>(
                    filterAllNewMessageEvent);
            foreach (var mea in logNewMessageEvents)
            {
                Console.WriteLine("newMessageEvent:\t" +
                                  mea.Event.sender + " " + mea.Event.ind.ToString() + " " + mea.Event.msg.ToString());
            }
        }

        public static async Task GetAllChangesExample(Web3 web3, string fromAddress, string fromPassword,
            string contractAddress, string contractAbi)
        {
            Console.WriteLine("GetAllChangesExample:");

            var contract = web3.Eth.GetContract(contractAbi, contractAddress);
            var newMessageEvent = contract.GetEvent("NewMessageEvent");
            var filterAllNewMessageEvent = await newMessageEvent.CreateFilterAsync(fromAddress);
            var logNewMessageEvents =
                await newMessageEvent
                    .GetAllChanges<FunctionOutputHelpers.NewMessageEventArgs>(filterAllNewMessageEvent);
            foreach (var mea in logNewMessageEvents)
            {
                Console.WriteLine("newMessageEvent:\t" +
                                  mea.Event.sender + " " + mea.Event.ind.ToString() + " " + mea.Event.msg.ToString());
            }
        }

        public static async Task GetContractValuesHistoryUniqueOffsetValueExample(Web3 web3, string contractAddress,
            HexBigInteger recentBlockNumber, ulong numberBlocks, int offset)
        {
            Console.WriteLine("GetContractValuesHistoryUniqueOffsetValueExample:");

            string previousValue = "";
            for (ulong blockNumber = (ulong) recentBlockNumber.Value;
                blockNumber > (ulong) recentBlockNumber.Value - numberBlocks;
                blockNumber--)
            {
                var blockNumberParameter = new Nethereum.RPC.Eth.DTOs.BlockParameter(blockNumber);
                var valueAtOffset = await web3.Eth.GetStorageAt.SendRequestAsync(contractAddress,
                    new HexBigInteger(offset), blockNumberParameter);
                if (valueAtOffset != previousValue)
                {
                    var block =
                        await web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(blockNumberParameter);
                    DateTime blockDateTime = Helpers.UnixTimeStampToDateTime((double) block.Timestamp.Value);
                    Console.WriteLine("blockDateTime:\t" + blockDateTime.ToString());

                    for (int storageOffset = 0; storageOffset < offset + 2; storageOffset++)
                    {
                        var valueAt = await web3.Eth.GetStorageAt.SendRequestAsync(contractAddress,
                            new HexBigInteger(storageOffset), blockNumberParameter);
                        Console.WriteLine("value:\t" + blockNumber.ToString() + " " + storageOffset.ToString() + " " +
                                          valueAt + " " + Helpers.ConvertHex(valueAt.Substring(2)));
                    }

                    previousValue = valueAtOffset;
                }
            }
        }
    }
}