using System;

namespace Coracle.Raft.Engine.Exceptions
{
    public class CoracleNodeAlreadyStartedException : Exception
    {
        public string NodeId { get; }
        public CoracleNodeAlreadyStartedException(string nodeId) : base($"Coracle: Node '{nodeId}' already started")
        {
            NodeId = nodeId;
        }

        public static CoracleNodeAlreadyStartedException New(string nodeId)
        {
            return new CoracleNodeAlreadyStartedException(nodeId);
        }
    }
}
