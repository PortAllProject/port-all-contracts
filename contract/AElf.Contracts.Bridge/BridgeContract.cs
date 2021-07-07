using AElf.Contracts.MultiToken;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Bridge
{
    public partial class BridgeContract : BridgeContractContainer.BridgeContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(State.MerkleTreeRecorderContract.Value == null, "Already initialized.");
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.OracleContract.Value = input.OracleContractAddress;
            State.MerkleTreeRecorderContract.Value = input.MerkleTreeRecorderContractAddress;
            State.RegimentContract.Value = input.RegimentContractAddress;
            return new Empty();
        }

        public override Empty CreateBridge(CreateBridgeInput input)
        {
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