using AElf.Contracts.NFT;
using AElf.Types;

namespace AElf.Contracts.Lottery
{
    public partial class LotteryContract
    {
        private string GetTokenSymbol()
        {
            return State.TokenSymbol.Value ?? Context.Variables.NativeSymbol;
        }

        private void DoTransfer(Address from, Address to, long amount, string symbol, long tokenId = 0)
        {
            if (symbol.Length >= MinimumNftTokenSymbolLength)
            {
                if (from == Context.Self)
                {
                    State.NFTContract.Transfer.Send(new TransferInput
                    {
                        Symbol = symbol,
                        TokenId = tokenId,
                        To = to,
                        Amount = amount
                    });
                }
                else
                {
                    State.NFTContract.TransferFrom.Send(new TransferFromInput
                    {
                        From = from,
                        Symbol = symbol,
                        TokenId = tokenId,
                        To = to,
                        Amount = amount
                    });
                }
            }
            else
            {
                
            }
        }
    }
}