using System;

namespace Coracle.Raft.Engine.Exceptions
{
    public class CoracleNodeNotStartedException : Exception
    {
        public string NodeId { get; }
        public CoracleNodeNotStartedException(string nodeId) : base($"Coracle: Node '{nodeId}' not yet started")
        {
            NodeId = nodeId;
        }

        public static CoracleNodeNotStartedException New(string nodeId)
        {
            return new CoracleNodeNotStartedException(nodeId);
        }
    }
}
