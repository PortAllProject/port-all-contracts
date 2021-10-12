using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Oracle;
using AElf.ContractTestKit;
using AElf.Types;
using Xunit;

namespace AElf.Contracts.Bridge.Tests
{
    public class BridgeContractTests : BridgeContractTestBase
    {
        private readonly Address _regimentAddress =
            Address.FromBase58("fsEW3n8zHD1g4rMKouMFMaXfR1d155ag5N1eoXEiLNQH56aKy");

        private Hash _swapHash;

        [Fact]
        public async Task PipelineTest()
        {
            await InitialSwapAsync();

            // Query
            var queryId = await MakeQueryAsync("ELF", 0, 4);

            // Commit
            await CommitAndRevealAsync("ELF", queryId, 0, 0, 4);

            // Swap
            await ReceiverBridgeContractStubs.First().SwapToken.SendAsync(new SwapTokenInput
            {
                OriginAmount = SampleSwapInfo.SwapInfos[0].OriginAmount,
                ReceiptId = SampleSwapInfo.SwapInfos[0].ReceiptId,
                SwapId = _swapHash
            });
        }

        private async Task CommitAndRevealAsync(string symbol, Hash queryId, long recorderId, long from, int count)
        {
            var receiptHashMap = new ReceiptHashMap
            {
                RecorderId = recorderId
            };
            for (var receiptId = from; receiptId < count + from; receiptId++)
            {
                receiptHashMap.Value.Add(receiptId, SampleSwapInfo.SwapInfos[(int) receiptId].ReceiptHash.ToString());
            }

            var salt = HashHelper.ComputeFrom("Salt");

            foreach (var account in Transmitters)
            {
                var stub = GetOracleContractStub(account.KeyPair);
                var commitInput = new CommitInput
                {
                    QueryId = queryId,
                    Commitment = HashHelper.ConcatAndCompute(
                        HashHelper.ComputeFrom(receiptHashMap.ToString()),
                        HashHelper.ConcatAndCompute(salt, HashHelper.ComputeFrom(account.Address)))
                };
                await stub.Commit.SendAsync(commitInput);
            }

            foreach (var stub in TransmittersOracleContractStubs.Take(3))
            {
                await stub.Reveal.SendAsync(new RevealInput
                {
                    Data = receiptHashMap.ToString(),
                    Salt = salt,
                    QueryId = queryId
                });
            }
        }

        private async Task<Hash> MakeQueryAsync(string symbol, long from, long count)
        {
            var queryInput = new QueryInput
            {
                Payment = 10000,
                QueryInfo = new QueryInfo
                {
                    Title = $"record_receipts_{symbol}",
                    Options = {from.ToString(), count.ToString()}
                },
                AggregatorContractAddress = StringAggregatorContractAddress,
                CallbackInfo = new CallbackInfo
                {
                    ContractAddress = BridgeContractAddress,
                    MethodName = "RecordReceiptHash"
                },
                DesignatedNodeList = new AddressList
                {
                    Value = {_regimentAddress}
                }
            };
            var executionResult = await TransmittersOracleContractStubs.First().Query.SendAsync(queryInput);
            return executionResult.Output;
        }

        private async Task InitialSwapAsync()
        {
            await InitializeOracleContractAsync();
            await InitializeBridgeContractAsync();

            // Create regiment.
            await OracleContractStub.CreateRegiment.SendAsync(new CreateRegimentInput
            {
                Manager = DefaultSenderAddress,
                InitialMemberList = {Transmitters.Select(a => a.Address)}
            });

            // Create swap.
            var createSwapResult = await BridgeContractStub.CreateSwap.SendAsync(new CreateSwapInput
            {
                OriginTokenNumericBigEndian = true,
                OriginTokenSizeInByte = 32,
                RegimentAddress = _regimentAddress
            });
            _swapHash = createSwapResult.Output;

            // Create PORT token.
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                TokenName = "Port Token",
                Decimals = 8,
                Issuer = DefaultSenderAddress,
                IsBurnable = true,
                Symbol = "PORT",
                TotalSupply = 10_00000000_00000000
            });

            // Issue PORT token.
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                To = Transmitters.First().Address,
                Amount = 10_00000000_00000000,
                Symbol = "PORT"
            });

            // Approve Oracle Contract.
            var transmitterTokenContractStub = GetTokenContractStub(Transmitters.First().KeyPair);
            await transmitterTokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = "PORT",
                Amount = 10_00000000_00000000,
                Spender = OracleContractAddress
            });
        }

        private async Task InitializeOracleContractAsync()
        {
            await OracleContractStub.Initialize.SendAsync(new Oracle.InitializeInput
            {
                RegimentContractAddress = RegimentContractAddress
            });
        }

        private async Task InitializeBridgeContractAsync()
        {
            await BridgeContractStub.Initialize.SendAsync(new InitializeInput
            {
                MerkleTreeGeneratorContractAddress = MerkleTreeGeneratorContractAddress,
                MerkleTreeRecorderContractAddress = MerkleTreeRecorderContractAddress,
                MerkleTreeLeafLimit = 16,
                OracleContractAddress = OracleContractAddress,
                RegimentContractAddress = RegimentContractAddress
            });
        }
    }
}