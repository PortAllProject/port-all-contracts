using System;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS13;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContract : OracleContractContainer.OracleContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            InitializeContractReferences();

            // Controller will be the sender by default.
            State.Controller.Value = Context.Sender;

            input.MinimumOracleNodesCount = input.MinimumOracleNodesCount == 0
                ? DefaultMinimumOracleNodesCount
                : input.MinimumOracleNodesCount;
            input.DefaultRevealThreshold = input.DefaultRevealThreshold == 0
                ? DefaultRevealThreshold
                : input.DefaultRevealThreshold;
            input.DefaultAggregateThreshold = input.DefaultAggregateThreshold == 0
                ? DefaultAggregateThreshold
                : input.DefaultAggregateThreshold;

            State.IsChargeFee.Value = input.IsChargeFee;

            Assert(input.MinimumOracleNodesCount >= input.DefaultRevealThreshold,
                "MinimumOracleNodesCount should be greater than or equal to DefaultRevealThreshold.");
            Assert(input.DefaultRevealThreshold >= input.DefaultAggregateThreshold,
                "DefaultRevealThreshold should be greater than or equal to DefaultAggregateThreshold.");
            Assert(input.DefaultAggregateThreshold > 0, "DefaultAggregateThreshold should be positive.");

            State.DefaultExpirationSeconds.Value =
                input.DefaultExpirationSeconds == 0 ? DefaultExpirationSeconds : input.DefaultExpirationSeconds;
            State.RevealThreshold.Value = input.DefaultRevealThreshold;
            State.AggregateThreshold.Value = input.DefaultAggregateThreshold;
            State.MinimumOracleNodesCount.Value = input.MinimumOracleNodesCount;
            State.Initialized.Value = true;
            return new Empty();
        }

        /// <summary>
        /// For testing.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty InitializeAndCreateToken(InitializeInput input)
        {
            Initialize(input);
            CreateToken();
            return new Empty();
        }

        public override Hash Query(QueryInput input)
        {
            var queryId = Context.GenerateId(HashHelper.ComputeFrom(input));
            var expirationTimestamp = Context.CurrentBlockTime.AddSeconds(State.DefaultExpirationSeconds.Value);

            // Transfer tokens to virtual address for this query.
            if (input.Payment > 0)
            {
                var virtualAddress = Context.ConvertVirtualAddressToContractAddress(queryId);
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = virtualAddress,
                    Amount = input.Payment,
                    Symbol = TokenSymbol
                });
            }

            Assert(State.QueryRecords[queryId] == null, "Query already exists.");

            var designatedNodeList = GetActualDesignatedNodeList(input.DesignatedNodeList);
            Assert(designatedNodeList.Value.Count >= State.MinimumOracleNodesCount.Value,
                $"Invalid designated nodes count, should at least be {State.MinimumOracleNodesCount.Value}.");

            var callbackInfo = input.CallbackInfo ?? new CallbackInfo
            {
                ContractAddress = Context.Self,
                MethodName = "NotSetCallbackInfo"
            };
            var queryRecord = new QueryRecord
            {
                QueryId = queryId,
                QuerySender = Context.Sender,
                AggregatorContractAddress = input.AggregatorContractAddress,
                DesignatedNodeList = input.DesignatedNodeList,
                ExpirationTimestamp = expirationTimestamp,
                CallbackInfo = callbackInfo,
                Payment = input.Payment,
                AggregateThreshold = Math.Max(GetAggregateThreshold(designatedNodeList.Value.Count),
                    input.AggregateThreshold),
                QueryInfo = input.QueryInfo,
                Token = input.Token,
            };
            State.QueryRecords[queryId] = queryRecord;

            Context.Fire(new QueryCreated
            {
                QueryId = queryRecord.QueryId,
                QuerySender = queryRecord.QuerySender,
                AggregatorContractAddress = queryRecord.AggregatorContractAddress,
                DesignatedNodeList = queryRecord.DesignatedNodeList,
                CallbackInfo = queryRecord.CallbackInfo,
                Payment = queryRecord.Payment,
                AggregateThreshold = queryRecord.AggregateThreshold,
                QueryInfo = queryRecord.QueryInfo,
                Token = queryRecord.Token,
            });

            return queryId;
        }

        private AddressList GetActualDesignatedNodeList(AddressList designatedNodeList)
        {
            if (designatedNodeList.Value.Count != 1) return designatedNodeList;

            if (designatedNodeList.Value.First() == State.ParliamentContract.Value)
            {
                return new AddressList
                {
                    Value =
                    {
                        State.ConsensusContract.GetCurrentMinerList.Call(new Empty()).Pubkeys
                            .Select(p => Address.FromPublicKey(p.ToByteArray()))
                    }
                };
            }

            var organization =
                State.AssociationContract.GetOrganization.Call(designatedNodeList.Value.First());
            if (organization == null)
            {
                throw new AssertionException("Designated association not exists.");
            }

            designatedNodeList = new AddressList
            {
                Value = {organization.OrganizationMemberList.OrganizationMembers}
            };
            return designatedNodeList;
        }

        private AddressList GetDesignatedNodeList(Hash queryId)
        {
            var queryRecord = State.QueryRecords[queryId];
            return queryRecord == null
                ? new AddressList()
                : GetActualDesignatedNodeList(queryRecord.DesignatedNodeList);
        }

        public override Empty Commit(CommitInput input)
        {
            var queryRecord = State.QueryRecords[input.QueryId];

            Assert(queryRecord.ExpirationTimestamp > Context.CurrentBlockTime, "Query expired.");
            Assert(!queryRecord.IsCancelled, "Query already cancelled.");

            // Confirm this query is still in Commit stage.
            Assert(!queryRecord.IsCommitStageFinished, "Commit stage of this query is already finished.");

            // Permission check.
            var designatedNodeListCount = queryRecord.DesignatedNodeList.Value.Count;
            bool isSenderInDesignatedNodeList;
            int actualNodeListCount; // Will use this variable later.
            if (designatedNodeListCount != 1)
            {
                isSenderInDesignatedNodeList = queryRecord.DesignatedNodeList.Value.Contains(Context.Sender);
                actualNodeListCount = queryRecord.DesignatedNodeList.Value.Count;
            }
            else
            {
                var organization =
                    State.AssociationContract.GetOrganization.Call(queryRecord.DesignatedNodeList.Value.First());
                isSenderInDesignatedNodeList =
                    organization.OrganizationMemberList.OrganizationMembers.Contains(Context.Sender);
                actualNodeListCount = organization.OrganizationMemberList.OrganizationMembers.Count;
            }

            Assert(isSenderInDesignatedNodeList, "No permission to commit for this query.");
            Assert(actualNodeListCount >= State.MinimumOracleNodesCount.Value, "Invalid designated nodes count.");

            var updatedResponseCount = State.ResponseCount[input.QueryId].Add(1);
            State.CommitmentMap[input.QueryId][Context.Sender] = input.Commitment;

            if (updatedResponseCount >= GetRevealThreshold(actualNodeListCount, queryRecord.AggregateThreshold))
            {
                // Move to next stage: Reveal
                queryRecord.IsSufficientCommitmentsCollected = true;
                State.ResponseCount[input.QueryId] = 0;

                Context.Fire(new SufficientCommitmentsCollected
                {
                    QueryId = input.QueryId
                });
            }
            else
            {
                State.ResponseCount[input.QueryId] = updatedResponseCount;
            }

            queryRecord.CommitmentsCount = queryRecord.CommitmentsCount.Add(1);
            State.QueryRecords[input.QueryId] = queryRecord;

            Context.Fire(new Committed
            {
                OracleNodeAddress = Context.Sender,
                QueryId = input.QueryId,
                Commitment = input.Commitment
            });

            return new Empty();
        }

        public override Empty Reveal(RevealInput input)
        {
            if (input.Data == null || input.Salt == null)
            {
                throw new AssertionException($"Invalid input: {input}");
            }

            var queryRecord = State.QueryRecords[input.QueryId];

            Assert(queryRecord.ExpirationTimestamp > Context.CurrentBlockTime, "Query expired.");
            Assert(!queryRecord.IsCancelled, "Query already cancelled.");

            // Confirm this query is in stage Commit.
            Assert(queryRecord.IsSufficientCommitmentsCollected, "This query hasn't collected sufficient commitments.");
            Assert(!queryRecord.IsSufficientDataCollected, "Query already finished.");

            // Permission check.
            var commitment = State.CommitmentMap[input.QueryId][Context.Sender];
            if (commitment == null)
            {
                throw new AssertionException(
                    "No permission to reveal for this query. Sender hasn't submit commitment.");
            }

            var dataHash = HashHelper.ComputeFrom(input.Data.ToByteArray());

            // Check commitment.
            if (HashHelper.ConcatAndCompute(dataHash,
                    HashHelper.ConcatAndCompute(input.Salt, HashHelper.ComputeFrom(Context.Sender.ToBase58()))) !=
                commitment)
            {
                Context.Fire(new CommitmentRevealFailed
                {
                    QueryId = input.QueryId,
                    Commitment = commitment,
                    Data = input.Data,
                    Salt = input.Salt,
                    OracleNodeAddress = Context.Sender
                });
                return new Empty();
            }

            Context.Fire(new CommitmentRevealed
            {
                QueryId = input.QueryId,
                Commitment = commitment,
                Data = input.Data,
                Salt = input.Salt,
                OracleNodeAddress = Context.Sender
            });

            if (!queryRecord.IsCommitStageFinished)
            {
                // Finish Commit stage anyway (because at least one oracle node revealed commitment after execution of this tx.)
                queryRecord.IsCommitStageFinished = true;
                // Maybe lessen the aggregate threshold.
                queryRecord.AggregateThreshold = Math.Min(queryRecord.AggregateThreshold, queryRecord.CommitmentsCount);
                State.QueryRecords[input.QueryId] = queryRecord;
            }

            // No need to count responses.
            State.ResponseCount.Remove(queryRecord.QueryId);

            var helpfulNodeList = State.HelpfulNodeListMap[input.QueryId] ?? new AddressList();
            Assert(!helpfulNodeList.Value.Contains(Context.Sender), "Sender already revealed commitment.");
            helpfulNodeList.Value.Add(Context.Sender);
            State.HelpfulNodeListMap[input.QueryId] = helpfulNodeList;

            if (queryRecord.AggregatorContractAddress != null)
            {
                // Record data to result list.
                var resultList = State.ResultListMap[input.QueryId] ?? new ResultList();
                if (resultList.Results.Contains(input.Data))
                {
                    var index = resultList.Results.IndexOf(input.Data);
                    resultList.Frequencies[index] = resultList.Frequencies[index].Add(1);
                }
                else
                {
                    resultList.Results.Add(input.Data);
                    resultList.Frequencies.Add(1);
                }

                State.ResultListMap[input.QueryId] = resultList;
            }
            else
            {
                // Record data to node data list.
                var nodeDataList = State.NodeDataListMap[input.QueryId] ?? new NodeDataList
                {
                    ObserverAssociationAddress = queryRecord.DesignatedNodeList.Value.First(),
                    QueryInfo = queryRecord.QueryInfo,
                    Token = queryRecord.Token
                };
                nodeDataList.Value.Add(new NodeData {Address = Context.Sender, Data = input.Data});
                State.NodeDataListMap[input.QueryId] = nodeDataList;
            }

            if (helpfulNodeList.Value.Count >= queryRecord.AggregateThreshold)
            {
                // Move to next stage: Aggregator.
                PayToNodesAndAggregateResults(queryRecord, helpfulNodeList);
            }

            return new Empty();
        }

        private void PayToNodesAndAggregateResults(QueryRecord queryRecord, AddressList helpfulNodeList)
        {
            queryRecord.IsSufficientDataCollected = true;
            // Distributed rewards to oracle nodes.
            foreach (var helpfulNode in helpfulNodeList.Value)
            {
                var paymentToEachNode = queryRecord.Payment.Div(helpfulNodeList.Value.Count);
                if (paymentToEachNode > 0)
                {
                    Context.SendVirtualInline(queryRecord.QueryId, State.TokenContract.Value,
                        nameof(State.TokenContract.Transfer), new TransferInput
                        {
                            To = helpfulNode,
                            Symbol = TokenSymbol,
                            Amount = paymentToEachNode
                        });
                }
            }

            var aggregatorContractAddress = queryRecord.AggregatorContractAddress;
            BytesValue finalResult;
            if (aggregatorContractAddress != null)
            {
                // Call Aggregator plugin contract.
                State.OracleAggregatorContract.Value = queryRecord.AggregatorContractAddress;
                var resultList = State.ResultListMap[queryRecord.QueryId];
                finalResult = State.OracleAggregatorContract.Aggregate.Call(new AggregateInput
                {
                    Results = {resultList.Results},
                    Frequencies = {resultList.Frequencies}
                });
                queryRecord.FinalResult = finalResult.Value;
            }
            else
            {
                // Give all the origin data provided by oracle nodes.
                finalResult = State.NodeDataListMap[queryRecord.QueryId].ToBytesValue();
            }

            // Update FinalResult field.
            State.QueryRecords[queryRecord.QueryId] = queryRecord;

            // Callback User Contract
            var callbackInfo = queryRecord.CallbackInfo;
            if (queryRecord.CallbackInfo.ContractAddress != Context.Self)
            {
                Context.SendInline(callbackInfo.ContractAddress, callbackInfo.MethodName, new CallbackInput
                {
                    QueryId = queryRecord.QueryId,
                    Result = finalResult.Value,
                    OracleNodes = {queryRecord.DesignatedNodeList.Value}
                });
            }

            Context.Fire(new QueryCompleted
            {
                QueryId = queryRecord.QueryId,
                Result = finalResult.Value
            });
        }

        public override Empty CancelQuery(Hash input)
        {
            var queryRecord = State.QueryRecords[input];
            if (queryRecord == null)
            {
                throw new AssertionException("Query not exists.");
            }

            Assert(queryRecord.QuerySender == Context.Sender, "No permission to cancel this query.");
            Assert(queryRecord.ExpirationTimestamp <= Context.CurrentBlockTime, "Query not expired.");
            Assert(!queryRecord.IsSufficientDataCollected && queryRecord.FinalResult.IsNullOrEmpty(),
                "Query already finished.");
            Assert(!queryRecord.IsCancelled, "Query already cancelled.");

            queryRecord.IsCancelled = true;

            State.QueryRecords[input] = queryRecord;

            if (queryRecord.Payment > 0 && State.IsChargeFee.Value)
            {
                // Return tokens to query manager.
                Context.SendVirtualInline(queryRecord.QueryId, State.TokenContract.Value,
                    nameof(State.TokenContract.Transfer), new TransferInput
                    {
                        To = queryRecord.QuerySender,
                        Symbol = TokenSymbol,
                        Amount = queryRecord.Payment
                    });
            }

            State.ResponseCount.Remove(input);
            State.HelpfulNodeListMap.Remove(input);
            if (queryRecord.CommitmentsCount > 0)
            {
                foreach (var address in GetActualDesignatedNodeList(queryRecord.DesignatedNodeList).Value)
                {
                    State.CommitmentMap[input].Remove(address);
                }
            }

            return new Empty();
        }

        private void InitializeContractReferences()
        {
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.ParliamentContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            State.AssociationContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.AssociationContractSystemName);
            State.ConsensusContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            State.ProfitContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
        }

        private void CreateToken()
        {
            var defaultParliament = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = TokenSymbol,
                TokenName = TokenName,
                IsBurnable = true,
                Issuer = defaultParliament,
                TotalSupply = TotalSupply
            });
        }
    }
}