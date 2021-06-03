using System;
using System.Linq;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Oracle
{
    public partial class OracleContract
    {
        private int GetRevealThreshold(int nodeCount, int inputAggregateThreshold = 0)
        {
            return Math.Max(Math.Max(nodeCount.Mul(2).Div(3).Add(1), State.RevealThreshold.Value),
                inputAggregateThreshold);
        }

        private int GetAggregateThreshold(int nodeCount)
        {
            return Math.Max(nodeCount.Div(3).Add(1), State.AggregateThreshold.Value);
        }
    }
}