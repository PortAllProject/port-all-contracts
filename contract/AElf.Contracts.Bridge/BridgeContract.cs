using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Bridge
{
    public partial class BridgeContract : BridgeContractContainer.BridgeContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.OracleContract.Value = input.OracleContractAddress;
            return new Empty();
        }

        public override Empty CreateBridge(CreateBridgeInput input)
        {
            // Need to pay.

            var bridgeTokenInfo = input.OriginTokenInfo;
            if (bridgeTokenInfo.IsNativeToken)
            {
                var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
                {
                    Symbol = input.WrappedTokenSymbol
                });
                Assert(!string.IsNullOrEmpty(tokenInfo.TokenName), $"Token {input.WrappedTokenSymbol} not found.");
                Assert(State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty()) == Context.Sender,
                    "No permission.");
            }

            var tokenId =
                $"{bridgeTokenInfo.FromChainName}/{bridgeTokenInfo.Symbol}/{bridgeTokenInfo.LockContractAddress}";
            State.BridgeTokenInfoMap[tokenId] = bridgeTokenInfo;

            Context.Fire(new BridgeCreated
            {
                OriginTokenInfo = input.OriginTokenInfo,
                WrappedTokenSymbol = input.WrappedTokenSymbol
            });
            return new Empty();
        }
    }
}