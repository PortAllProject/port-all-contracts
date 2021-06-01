using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContract
    {
        public override Empty ChangeController(Address input)
        {
            Assert(Context.Sender == State.Controller.Value, "Not authorized");
            State.Controller.Value = input;
            return new Empty();
        }

        public override Empty SetThreshold(OracleNodeThreshold input)
        {
            Assert(Context.Sender == State.Controller.Value, "Not authorized");
            Assert(input.MinimumOracleNodesCount >= input.DefaultRevealThreshold,
                "MinimumOracleNodesCount should be greater than or equal to DefaultRevealThreshold.");
            Assert(input.DefaultRevealThreshold >= input.DefaultAggregateThreshold,
                "DefaultRevealThreshold should be greater than or equal to DefaultAggregateThreshold.");
            Assert(input.DefaultAggregateThreshold > 0, "DefaultAggregateThreshold should be positive.");
            State.MinimumOracleNodesCount.Value = input.MinimumOracleNodesCount;
            State.RevealThreshold.Value = input.DefaultRevealThreshold;
            State.AggregateThreshold.Value = input.DefaultAggregateThreshold;
            return new Empty();
        }

        public override Empty ChangeDefaultExpirationSeconds(Int32Value input)
        {
            Assert(Context.Sender == State.Controller.Value, "Not authorized");
            State.DefaultExpirationSeconds.Value = input.Value;
            return new Empty();
        }

        public override OracleNodeThreshold GetThreshold(Empty input)
        {
            return new OracleNodeThreshold
            {
                MinimumOracleNodesCount = State.MinimumOracleNodesCount.Value,
                DefaultRevealThreshold = State.RevealThreshold.Value,
                DefaultAggregateThreshold = State.AggregateThreshold.Value
            };
        }

        public override Int32Value GetDefaultExpirationSeconds(Empty input)
        {
            return new Int32Value {Value = State.DefaultExpirationSeconds.Value};
        }

        public override Empty EnableChargeFee(Empty input)
        {
            Assert(Context.Sender == State.Controller.Value, "Not authorized");
            State.IsChargeFee.Value = true;
            return new Empty();
        }

        public override Empty AddPostPayAddress(Address input)
        {
            Assert(Context.Sender == State.Controller.Value, "Not authorized");
            Assert(!State.PostPayAddressMap[input], "Already added.");
            State.PostPayAddressMap[input] = true;
            return new Empty();
        }

        public override Empty RemovePostPayAddress(Address input)
        {
            Assert(Context.Sender == State.Controller.Value, "Not authorized");
            Assert(State.PostPayAddressMap[input], "Not a post pay address.");
            State.PostPayAddressMap.Remove(input);
            return new Empty();
        }
    }
}