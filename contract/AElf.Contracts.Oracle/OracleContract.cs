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
            State.RegimentContract.Value = input.RegimentContractAddress;
            State.RegimentContract.Initialize.Send(new Regiment.InitializeInput
            {
                Controller = Context.Self
            });

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

            Assert(State.QueryRecords[queryId] == null, "Query already exists.");

            var designatedNodeList = GetActualDesignatedNodeList(input.DesignatedNodeList);
            Assert(designatedNodeList.Value.Count >= State.MinimumOracleNodesCount.Value,
                $"Invalid designated nodes count, should at least be {State.MinimumOracleNodesCount.Value}.");

            var callbackInfo = input.CallbackInfo ?? new CallbackInfo
            {
                ContractAddress = Context.Self,
                MethodName = NotSetCallbackInfo
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
                AggregateOption = input.AggregateOption,
                TaskId = input.TaskId ?? Hash.Empty
            };
            
            // Transfer tokens to virtual address for this query.
            if (!State.PostPayAddressMap[Context.Sender])
            {
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

                queryRecord.IsPaidToOracleContract = true;
            }
            else
            {
                Assert(
                    State.TokenContract.GetBalance.Call(new GetBalanceInput
                    {
                        Owner = Context.Sender, Symbol = TokenSymbol
                    }).Balance >= input.Payment, "Insufficient balance for payment.");
                Assert(State.TokenContract.GetAllowance.Call(new GetAllowanceInput
                {
                    Owner = Context.Sender, Spender = Context.Self, Symbol = TokenSymbol
                }).Allowance >= input.Payment, "Insufficient allowance for payment.");
            }

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
                AggregateOption = queryRecord.AggregateOption,
                TaskId = queryRecord.TaskId
            });

            return queryId;
        }

        public override Hash CreateQueryTask(CreateQueryTaskInput input)
        {
            // TODO: Pay tx fee to contract.

            var taskId = Context.TransactionId;
            var queryTask = new QueryTask
            {
                Creator = Context.Sender,
                CallbackInfo = input.CallbackInfo,
                EachPayment = input.EachPayment,
                SupposedQueryTimes = input.SupposedQueryTimes,
                QueryInfo = input.QueryInfo,
                EndTime = input.EndTime,
                AggregatorContractAddress = input.AggregatorContractAddress,
                AggregateOption = input.AggregateOption,
                AggregateThreshold = input.AggregateThreshold
            };
            State.QueryTaskMap[taskId] = queryTask;
            
            Context.Fire(new QueryTaskCreated
            {
                Creator = queryTask.Creator,
                CallbackInfo = queryTask.CallbackInfo,
                EachPayment = queryTask.EachPayment,
                SupposedQueryTimes = queryTask.SupposedQueryTimes,
                QueryInfo = queryTask.QueryInfo,
                EndTime = queryTask.EndTime,
                AggregatorContractAddress = queryTask.AggregatorContractAddress,
                AggregateOption = queryTask.AggregateOption,
                AggregateThreshold = queryTask.AggregateThreshold
            });
            return taskId;
        }

        public override Empty CompleteQueryTask(CompleteQueryTaskInput input)
        {
            var queryTask = State.QueryTaskMap[input.TaskId];
            if (queryTask == null)
            {
                throw new AssertionException("Query task not found.");
            }

            Assert(queryTask.DesignatedNodeList == null, "Designated node list already assigned.");
            Assert(Context.Sender == queryTask.Creator, "No permission.");

            var designatedNodeList = GetActualDesignatedNodeList(input.DesignatedNodeList);
            Assert(designatedNodeList.Value.Count >= State.MinimumOracleNodesCount.Value,
                $"Invalid designated nodes count, should at least be {State.MinimumOracleNodesCount.Value}.");

            queryTask.DesignatedNodeList = input.DesignatedNodeList;
            queryTask.AggregateThreshold = Math.Max(GetAggregateThreshold(designatedNodeList.Value.Count),
                input.AggregateThreshold);
            State.QueryTaskMap[input.TaskId] = queryTask;
            return new Empty();
        }

        public override Hash TaskQuery(TaskQueryInput input)
        {
            var queryTask = State.QueryTaskMap[input.TaskId];
            if (queryTask == null)
            {
                throw new AssertionException("Query task not found.");
            }

            Assert(Context.Sender == queryTask.Creator, "No permission.");
            Assert(queryTask.OnGoing == false, "Previous query not finished.");
            Assert(queryTask.ActualQueriedTimes < queryTask.SupposedQueryTimes, "Query times exceeded.");

            queryTask.OnGoing = true;
            State.QueryTaskMap[input.TaskId] = queryTask;

            var queryInput = new QueryInput
            {
                Payment = queryTask.EachPayment,
                AggregateThreshold = queryTask.AggregateThreshold,
                AggregatorContractAddress = queryTask.AggregatorContractAddress,
                CallbackInfo = queryTask.CallbackInfo,
                DesignatedNodeList = queryTask.DesignatedNodeList,
                QueryInfo = queryTask.QueryInfo,
                AggregateOption = queryTask.AggregateOption,
                TaskId = input.TaskId,
                Token = $"{input.TaskId.ToHex()}:{queryTask.ActualQueriedTimes}"
            };

            return Query(queryInput);
        }

        private AddressList GetActualDesignatedNodeList(AddressList designatedNodeList)
        {
            if (designatedNodeList.Value.Count != 1) return designatedNodeList;
            var regimentAddress = designatedNodeList.Value.First();
            return GetRegimentMemberList(regimentAddress);
        }

        private AddressList GetActualDesignatedNodeList(Hash queryId)
        {
            var queryRecord = State.QueryRecords[queryId];
            return queryRecord == null
                ? new AddressList()
                : GetActualDesignatedNodeList(queryRecord.DesignatedNodeList);
        }

        public override Empty Commit(CommitInput input)
        {
            var queryRecord = State.QueryRecords[input.QueryId];

            if (queryRecord == null)
            {
                throw new AssertionException("Query id not exists.");
            }

            Assert(queryRecord.ExpirationTimestamp > Context.CurrentBlockTime, "Query expired.");
            Assert(!queryRecord.IsCancelled, "Query already cancelled.");

            // Confirm this query is still in Commit stage.
            Assert(!queryRecord.IsCommitStageFinished, "Commit stage of this query is already finished.");

            // Permission check.
            var actualDesignatedNodeList = GetActualDesignatedNodeList(queryRecord.DesignatedNodeList);
            Assert(actualDesignatedNodeList.Value.Contains(Context.Sender), "Sender is not in designated node list.");

            Assert(actualDesignatedNodeList.Value.Count >= State.MinimumOracleNodesCount.Value,
                "Invalid designated nodes count.");

            var updatedResponseCount = State.ResponseCount[input.QueryId].Add(1);
            State.CommitmentMap[input.QueryId][Context.Sender] = input.Commitment;

            if (updatedResponseCount >=
                GetRevealThreshold(actualDesignatedNodeList.Value.Count, queryRecord.AggregateThreshold))
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

            // Stage check.
            Assert(queryRecord.IsSufficientCommitmentsCollected, "This query hasn't collected sufficient commitments.");
            Assert(!queryRecord.IsSufficientDataCollected, "Query already finished.");
            var commitment = State.CommitmentMap[input.QueryId][Context.Sender];
            if (commitment == null)
            {
                throw new AssertionException(
                    "No permission to reveal for this query. Sender hasn't submit commitment.");
            }

            // Permission check.
            var actualDesignatedNodeList = GetActualDesignatedNodeList(queryRecord.DesignatedNodeList);
            Assert(actualDesignatedNodeList.Value.Contains(Context.Sender),
                "Sender was removed from designated node list.");

            var dataHash = HashHelper.ComputeFrom(input.Data);

            // Check commitment.
            var supposedCommitment = HashHelper.ConcatAndCompute(dataHash,
                HashHelper.ConcatAndCompute(input.Salt, HashHelper.ComputeFrom(Context.Sender.ToBase58())));
            if (supposedCommitment != commitment)
            {
                Context.Fire(new CommitmentRevealFailed
                {
                    QueryId = input.QueryId,
                    Commitment = commitment,
                    RevealData = input.Data,
                    Salt = input.Salt,
                    OracleNodeAddress = Context.Sender
                });
                return new Empty();
            }

            Context.Fire(new CommitmentRevealed
            {
                QueryId = input.QueryId,
                Commitment = commitment,
                RevealData = input.Data,
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

            // Reorg helpful nodes list.
            helpfulNodeList = new AddressList
            {
                Value = {helpfulNodeList.Value.Where(a => actualDesignatedNodeList.Value.Contains(a))}
            };

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
                var nodeDataList = State.PlainResultMap[input.QueryId] ?? new PlainResult
                {
                    RegimentAddress = queryRecord.DesignatedNodeList.Value.First(),
                    QueryInfo = queryRecord.QueryInfo,
                    Token = queryRecord.Token,
                    DataRecords = new DataRecords()
                };
                nodeDataList.DataRecords.Value.Add(new DataRecord {Address = Context.Sender, Data = input.Data});
                State.PlainResultMap[input.QueryId] = nodeDataList;
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
            // Post pay.
            if (!queryRecord.IsPaidToOracleContract)
            {
                var virtualAddress = Context.ConvertVirtualAddressToContractAddress(queryRecord.QueryId);
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = queryRecord.QuerySender,
                    To = virtualAddress,
                    Amount = queryRecord.Payment,
                    Symbol = TokenSymbol
                });
            }

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
                var finalResultStr = State.OracleAggregatorContract.Aggregate.Call(new AggregateInput
                {
                    Results = {resultList.Results},
                    Frequencies = {resultList.Frequencies},
                    AggregateOption = queryRecord.AggregateOption
                }).Value;
                finalResult = new StringValue {Value = finalResultStr}.ToBytesValue();
                queryRecord.FinalResult = finalResultStr;

                Context.Fire(new QueryCompletedWithAggregation
                {
                    QueryId = queryRecord.QueryId,
                    Result = finalResultStr
                });
            }
            else
            {
                // Give all the origin data provided by oracle nodes.
                var plainResult = State.PlainResultMap[queryRecord.QueryId];
                finalResult = plainResult.ToBytesValue();
                Context.Fire(new QueryCompletedWithoutAggregation
                {
                    QueryId = queryRecord.QueryId,
                    Result = plainResult
                });
            }

            // Update FinalResult field.
            State.QueryRecords[queryRecord.QueryId] = queryRecord;

            // Callback User Contract
            var callbackInfo = queryRecord.CallbackInfo;
            if (callbackInfo.ContractAddress != Context.Self)
            {
                Context.SendInline(callbackInfo.ContractAddress, callbackInfo.MethodName, new CallbackInput
                {
                    QueryId = queryRecord.QueryId,
                    Result = finalResult.Value,
                    OracleNodes = {queryRecord.DesignatedNodeList.Value}
                }); 
            }

            // If this query is from a query task.
            if (queryRecord.TaskId != Hash.Empty)
            {
                var queryTask = State.QueryTaskMap[queryRecord.TaskId];
                queryTask.OnGoing = false;
                queryTask.ActualQueriedTimes = queryTask.ActualQueriedTimes.Add(1);
                State.QueryTaskMap[queryRecord.TaskId] = queryTask;
            }
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
            Assert(
                !queryRecord.IsSufficientDataCollected && string.IsNullOrEmpty(queryRecord.FinalResult) &&
                (queryRecord.DataRecords == null || queryRecord.DataRecords.Value.Count == 0),
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

            Context.Fire(new QueryCancelled
            {
                QueryId = input
            });
            return new Empty();
        }

        private void InitializeContractReferences()
        {
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.ParliamentContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
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