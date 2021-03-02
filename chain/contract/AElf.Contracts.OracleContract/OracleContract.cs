using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS13;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.OracleContract
{
    public partial class OracleContract : OracleContractContainer.OracleContractBase
    {
        public override Empty CreateRequest(CreateRequestInput input)
        {
            var expiration = Context.CurrentBlockTime.AddSeconds(State.ExpirationTime.Value);
            var requestId = GenerateRequestId(Context.Sender, input.Nonce);
            long payment = input.Payment;
            var callbackAddress = input.CallbackAddress;
            var methodName = input.MethodName;
            var aggregator = input.Aggregator;
            var paramsHash = GenerateParamHash(payment, callbackAddress, methodName, expiration);
            Assert(State.Commitments[requestId] == null, "Repeated request");
            var designatedNodes = input.DesignatedNodes;
            var designatedNodesCount = designatedNodes.NodeList.Count;
            if (designatedNodesCount > 0)
            {
                Assert(
                    designatedNodesCount >= State.MinimumResponses.Value &&
                    designatedNodesCount <= State.AvailableNodes.Value.NodeList.Count,
                    "Invalid count of designated nodes");
            }

            State.Commitments[requestId] = new Commitment
            {
                ParamsHash = paramsHash,
                DataVersion = input.DataVersion,
                DesignatedNodes = designatedNodes,
                Aggregator = input.Aggregator
            };
            var roundCount = State.AnswerCounter[requestId];
            roundCount = roundCount.Add(1);
            State.AnswerCounter[requestId] = roundCount;
            State.Answers[requestId][roundCount] = new Answer();
            Context.Fire(new NewRequest
            {
                Requester = Context.Sender,
                RequestId = requestId,
                Payment = payment,
                CallbackAddress = callbackAddress,
                MethodName = methodName,
                CancelExpiration = expiration,
                UrlToQuery = input.UrlToQuery,
                AttributeToFetch = input.AttributeToFetch,
                Aggregator = aggregator
            });

            return new Empty();
        }

        public override Empty SendHashData(SendHashDataInput input)
        {
            var requestId = input.RequestId;
            VerifyRequest(requestId, input.Payment, input.CallbackAddress, input.MethodName, input.CancelExpiration);
            var currentRoundCount = State.AnswerCounter[requestId];
            var answer = State.Answers[requestId][currentRoundCount];
            int minimumHashData = State.MinimumResponses.Value;
            Assert(answer.HashDataResponses < minimumHashData,
                $"Enough hash data for request {requestId}");
            VerifyNode(requestId, Context.Sender);
            var lastHashData = State.HashData[requestId][Context.Sender];
            State.HashData[requestId][Context.Sender] = input.HashData;
            if (lastHashData != null)
            {
                return new Empty();
            }

            answer.HashDataResponses = answer.HashDataResponses.Add(1);
            State.Answers[requestId][currentRoundCount] = answer;
            if (answer.HashDataResponses == minimumHashData)
            {
                Context.Fire(new GetEnoughData
                {
                    RequestId = requestId
                });
            }

            return new Empty();
        }

        public override Empty SendDataWithSalt(SendDataWithSaltInput input)
        {
            var requestId = input.RequestId;
            VerifyRequest(requestId, input.Payment, input.CallbackAddress, input.MethodName, input.CancelExpiration);
            var currentRoundCount = State.AnswerCounter[requestId];
            var answers = State.Answers[requestId][currentRoundCount];
            Assert(answers.HashDataResponses == State.MinimumResponses.Value,
                $"Not enough hash data received for request {requestId}");
            Assert(answers.DataWithSaltResponses < State.MinimumResponses.Value,
                $"Enough real data received for request {requestId}");
            VerifyNode(requestId, Context.Sender);
            var nodeRealData = answers.Responses;
            if (nodeRealData.Any(x => x.Node == Context.Sender))
            {
                return new Empty();
            }

            VerifyHashDataWithSalt(requestId, Context.Sender, input.RealData, input.Salt);
            nodeRealData.Add(new NodeWithData
            {
                Node = Context.Sender,
                RealData = input.RealData
            });
            answers.DataWithSaltResponses = answers.DataWithSaltResponses.Add(1);
            State.Answers[requestId][currentRoundCount] = answers;
            if (answers.DataWithSaltResponses != State.MinimumResponses.Value) return new Empty();
            var aggregatorAddress = State.Commitments[requestId].Aggregator;
            if (aggregatorAddress != null)
            {
                // var aggregatorAnswer = new AElf.Standards.ACS13.
                // Context.SendInline(aggregatorAddress, "asd", answers);
                return new Empty();
            }

            if (AggregateData(nodeRealData, out var chooseData))
            {
                Context.Fire(new UpdatedRequest
                {
                    RequestId = requestId,
                    AgreedValue = chooseData
                });
                Context.SendInline(input.CallbackAddress, input.MethodName, chooseData);
                return new Empty();
            }

            DealQuestionableQuery(requestId, currentRoundCount, nodeRealData);
            return new Empty();
        }

        private bool AggregateData(IEnumerable<NodeWithData> allData, out ByteString chooseData)
        {
            var groupData = allData.GroupBy(x => x.RealData);
            var groupedData = groupData as IGrouping<ByteString, NodeWithData>[] ?? groupData.ToArray();
            var maxCount = groupedData.Max(x => x.Count());
            if (maxCount < State.ThresholdToUpdateData.Value)
            {
                chooseData = null;
                return false;
            }

            chooseData = groupedData.First(x => x.Count() == maxCount).Key;
            return true;
        }

        private void DealQuestionableQuery(Hash requestId, long roundId, IEnumerable<NodeWithData> allData)
        {
        }
    }
}