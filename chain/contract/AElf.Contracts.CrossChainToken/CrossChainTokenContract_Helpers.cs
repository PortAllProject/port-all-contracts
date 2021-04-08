using System.Text;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Contracts.CrossChainToken
{
    public partial class CrossChainTokenContract
    {

        private string GetTokenFullName(string symbol, string field)
        {
            return $"{symbol}.{field}";
        }

        private void DoTransfer(Address from, Address to, string tokenFullName, long amount, string memo = null)
        {
            Assert(from != to, "Can't do transfer to sender itself.");
            AssertValidMemo(memo);
            ModifyBalance(from, tokenFullName, -amount);
            ModifyBalance(to, tokenFullName, amount);
            Context.Fire(new Transferred
            {
                From = from,
                To = to,
                TokenFullName = tokenFullName,
                Amount = amount,
                Memo = memo ?? string.Empty
            });
        }

        private void AssertValidMemo(string memo)
        {
            Assert(memo == null || Encoding.UTF8.GetByteCount(memo) <= MemoMaximumLength,
                "Invalid memo size.");
        }

        private void ModifyBalance(Address address, string symbol, long addAmount)
        {
            var before = GetBalance(address, symbol);
            if (addAmount < 0 && before < -addAmount)
            {
                Assert(false,
                    $"Insufficient balance of {symbol}. Need balance: {-addAmount}; Current balance: {before}");
            }

            var target = before.Add(addAmount);
            State.Balances[address][symbol] = target;
        }

        private long GetBalance(Address address, string symbol)
        {
            return State.Balances[address][symbol];
        }

        private string GetQueryUrl(string ethereumTxId, string fromChainField)
        {
            // TODO: Find a way to query ethereum tx result.
            if (fromChainField == "ETH")
            {
                return ethereumTxId;
            }

            return string.Empty;
        }
    }
}